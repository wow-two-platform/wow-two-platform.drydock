"""Reviewed product definitions: the one place a product's identity lives. Adding a product is a code change."""
from __future__ import annotations
from dataclasses import dataclass
import re
from runner import SLUG, require

REPOSITORY = re.compile(r"[A-Za-z0-9](?:[A-Za-z0-9-]{0,38})/[A-Za-z0-9._-]{1,100}")
BRANCH = re.compile(r"[A-Za-z0-9][A-Za-z0-9._/-]{0,199}")


@dataclass(frozen=True)
class Release:
    """Where a product's published releases and commit builds come from."""
    # The asset every published GitHub release carries, such as `foreverpin-release.tar.gz`.
    asset: str
    # Service name -> image repository; the bundle's digests must come from exactly these.
    images: tuple[tuple[str, str], ...]
    # The workflow that builds a commit into a `bundle-<sha>` artifact; without one, only releases deploy.
    workflow: str | None = None


@dataclass(frozen=True)
class Product:
    slug: str
    name: str
    description: str
    # The GitHub repository that defines the product, as `owner/name`.
    repository: str
    default_branch: str = "main"
    # Without a release source, the product deploys only bundles imported by hand.
    release: Release | None = None


PRODUCTS: tuple[Product, ...] = (
    Product("foreverpin", "ForeverPin",
            "Styled QR codes and short links whose destination can change after printing.",
            "sulton-max/10x-venture-forever-pin",
            release=Release("foreverpin-release.tar.gz",
                            (("management", "ghcr.io/sulton-max/10x-venture-forever-pin/management"),
                             ("redirect", "ghcr.io/sulton-max/10x-venture-forever-pin/redirect")),
                            workflow="publish-docker-image.yml")),
    # Its console image is private, so it deploys `rehearse.py self` imports until targets can pull private images.
    Product("wheelhouse", "Wheelhouse",
            "The portfolio's deploy and operations control plane.",
            "wow-two-platform/wow-two-platform.wheelhouse"),
)


def products():
    """Validates the catalog and returns it in the order it is declared."""
    require(len({product.slug for product in PRODUCTS}) == len(PRODUCTS), "Duplicate product slug")
    for product in PRODUCTS:
        require(SLUG.fullmatch(product.slug), "Invalid product slug")
        require(0 < len(product.name.strip()) <= 80, "Invalid product name")
        require(0 < len(product.description.strip()) <= 200, "Invalid product description")
        require(REPOSITORY.fullmatch(product.repository), "Invalid product repository")
        require(BRANCH.fullmatch(product.default_branch), "Invalid default branch")
        if product.release is not None:
            services = [service for service, _ in product.release.images]
            require(len(set(services)) == len(services) and all(SLUG.fullmatch(name) for name in services),
                    "Invalid release services")
    return PRODUCTS


def product(slug):
    """Returns the product the catalog names `slug`; anything else is refused."""
    found = next((item for item in products() if item.slug == slug), None)
    require(found is not None, "Product is not defined in code")
    return found
