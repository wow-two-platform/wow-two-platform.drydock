"""Read-only discovery of completed release assets from code-owned public repositories."""
from dataclasses import dataclass
from enum import Enum
import hashlib
import json
import os
from pathlib import Path
import re
import tempfile
from urllib.parse import quote, urlparse
from urllib.request import Request, HTTPRedirectHandler, build_opener
from runner import require, validate_bundle, write_json


class ArtifactProvider(str, Enum):
    GITHUB_RELEASES = "GitHubReleases"


@dataclass(frozen=True)
class Source:
    product: str
    repository: str
    asset_name: str
    images: tuple[tuple[str, str], ...]
    provider: ArtifactProvider = ArtifactProvider.GITHUB_RELEASES


SOURCES = (Source("foreverpin", "sulton-max/10x-venture-forever-pin", "foreverpin-release.tar.gz",
                  (("management", "ghcr.io/sulton-max/10x-venture-forever-pin"),
                   ("redirect", "ghcr.io/sulton-max/10x-venture-forever-pin-redirect"))),)
VERSION = re.compile(r"v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?")
MAX_ARCHIVE = 3 * 1024 * 1024


class ApiRedirect(HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, message, headers, new_url):
        require(urlparse(new_url).scheme == "https", "Insecure release redirect")
        redirected = super().redirect_request(request, fp, code, message, headers, new_url)
        if redirected is not None and urlparse(new_url).netloc != urlparse(request.full_url).netloc:
            redirected.remove_header("Authorization")
        return redirected


def fetch(url, limit=MAX_ARCHIVE):
    headers = {"User-Agent": "Drydock-release-catalog", "X-GitHub-Api-Version": "2022-11-28"}
    token_file = os.environ.get("DRYDOCK_GITHUB_TOKEN_FILE")
    if token_file and urlparse(url).netloc == "api.github.com":
        token = Path(token_file).read_text().strip()
        require(token and "\n" not in token, "Invalid catalog credential")
        headers["Authorization"] = "Bearer " + token
    request = Request(url, headers=headers)
    with build_opener(ApiRedirect()).open(request, timeout=15) as response:
        require(response.url.startswith("https://"), "Insecure release response")
        data = response.read(limit + 1)
    require(len(data) <= limit, "Release response exceeded its size limit")
    return data


def available():
    result = []
    for source in SOURCES:
        require(source.provider is ArtifactProvider.GITHUB_RELEASES, "Unsupported artifact provider")
        # A bounded recent catalog; old deployed bundles remain in the target recovery journal.
        releases = json.loads(fetch("https://api.github.com/repos/" + source.repository + "/releases?per_page=100"))
        for release in releases:
            tag = release.get("tag_name", "")
            if release.get("draft") or not release.get("published_at") or not VERSION.fullmatch(tag):
                continue
            assets = [asset for asset in release.get("assets", []) if asset.get("name") == source.asset_name]
            if len(assets) != 1:
                continue
            asset = assets[0]
            if (asset.get("state") != "uploaded" or not 0 < asset.get("size", 0) <= MAX_ARCHIVE
                    or not re.fullmatch(r"sha256:[a-f0-9]{64}", asset.get("digest") or "")
                    or type(asset.get("id")) is not int or asset["id"] <= 0):
                continue
            result.append({"id": source.product + "-gh-" + str(asset["id"]), "product": source.product,
                           "release": tag, "repository": source.repository, "provider": source.provider.value,
                           "publishedAt": release["published_at"], "prerelease": bool(release.get("prerelease")),
                           "assetDigest": asset["digest"], "assetName": source.asset_name})
    return result


def prepare(root, identifier, importer):
    artifact = next((item for item in available() if item["id"] == identifier), None)
    require(artifact is not None, "Release artifact is no longer available")
    source = next(item for item in SOURCES if item.product == artifact["product"])
    bundle = Path(root) / "bundles" / identifier
    require(not bundle.is_symlink(), "Release cache cannot be a symlink")
    if not bundle.exists():
        url = ("https://github.com/" + source.repository + "/releases/download/"
               + quote(artifact["release"], safe="") + "/" + source.asset_name)
        payload = fetch(url)
        require("sha256:" + hashlib.sha256(payload).hexdigest() == artifact["assetDigest"], "Release asset changed")
        with tempfile.TemporaryDirectory() as directory:
            archive = Path(directory) / "release.tar.gz"
            archive.write_bytes(payload)
            # Validate the entire archive before publishing it to the durable cache.
            importer(Path(directory), archive, "candidate")
            validate_source(Path(directory) / "bundles/candidate", source, artifact)
            importer(root, archive, identifier)
        write_json(bundle / "source.json", artifact)
    else:
        receipt = json.loads((bundle / "source.json").read_text())
        require(receipt.get("assetDigest") == artifact["assetDigest"], "Release receipt changed")
    validate_source(bundle, source, artifact)
    return bundle


def validate_source(bundle, source, artifact):
    manifest = validate_bundle(bundle)
    require(manifest["product"] == source.product and manifest["release"] == artifact["release"], "Release identity mismatch")
    expected = dict(source.images)
    require(manifest["images"].keys() == expected.keys(), "Unexpected release service")
    for service, image in manifest["images"].items():
        require(image.split("@")[0] == expected[service], "Image is outside the approved repository")
    commit = json.loads(fetch("https://api.github.com/repos/" + source.repository + "/commits/"
                              + quote(artifact["release"], safe="")))
    require(commit.get("sha") == manifest["sourceCommit"], "Release source commit mismatch")
    return manifest
