// Normalizes whatever a user pastes into the repo field — a full GitHub URL, an SSH remote, or a
// bare owner/repo — down to the { provider, repo } the backend expects. Only GitHub is supported
// today; other hosts resolve to a typed error so the form can explain why and block submit.

/** Provider the parsed repo belongs to. Only GitHub is wired end-to-end today. */
export type RepoProvider = 'github';

/** Successful parse — a recognized host (or bare reference), reduced to `owner/repo`. */
export interface ParsedRepo {
  provider: RepoProvider;
  repo: string;
  error?: undefined;
}

/** Failed parse — an unsupported host or an unrecognizable value, with a user-facing reason. */
export interface UnparsedRepo {
  provider: null;
  error: string;
  repo?: undefined;
}

/** Result of {@link parseRepoInput}: either a normalized repo or a reason it couldn't be. */
export type RepoParseResult = ParsedRepo | UnparsedRepo;

/** Hosts we recognize but don't support yet — matched to give a precise "not supported" message. */
const UNSUPPORTED_HOSTS = ['gitlab.com', 'bitbucket.org'];

/** A valid GitHub path segment: letters, digits, dot, dash, underscore (no slashes or spaces). */
const SEGMENT = /^[\w.-]+$/;

/**
 * Reduces a repository reference to `{ provider: 'github', repo: 'owner/repo' }`.
 *
 * Accepts, stripping scheme / host / a trailing `.git`:
 * - `https://github.com/{owner}/{repo}` (and `…/{repo}.git`)
 * - `git@github.com:{owner}/{repo}.git`
 * - `github.com/{owner}/{repo}`
 * - bare `{owner}/{repo}`
 *
 * An unsupported host (e.g. GitLab, Bitbucket) or an unparseable value returns
 * `{ provider: null, error }`.
 */
export function parseRepoInput(value: string): RepoParseResult {
  const trimmed = value.trim();
  if (trimmed === '')
    return { provider: null, error: 'Enter owner/repo or a repository URL' };

  // SSH remote: git@host:owner/repo(.git)
  const ssh = /^git@([^:]+):(.+)$/.exec(trimmed);
  if (ssh) {
    const host = ssh[1].toLowerCase();
    const unsupported = unsupportedHostError(host);
    if (unsupported) return unsupported;
    if (host !== 'github.com')
      return invalid();
    return fromOwnerRepo(ssh[2]);
  }

  // Anything with a scheme or a leading host — strip both down to the path.
  const hostMatch = /^(?:https?:\/\/)?((?:www\.)?[a-z0-9.-]+\.[a-z]{2,})\/(.+)$/i.exec(trimmed);
  if (hostMatch) {
    const host = hostMatch[1].toLowerCase().replace(/^www\./, '');
    const unsupported = unsupportedHostError(host);
    if (unsupported) return unsupported;
    if (host !== 'github.com')
      return invalid();
    return fromOwnerRepo(hostMatch[2]);
  }

  // A scheme with an unrecognized / hostless shape — can't trust it.
  if (trimmed.includes('://'))
    return invalid();

  // Bare owner/repo.
  return fromOwnerRepo(trimmed);
}

/** Builds the "{host} isn't supported yet" failure for a known-unsupported host, else `null`. */
function unsupportedHostError(host: string): UnparsedRepo | null {
  return UNSUPPORTED_HOSTS.includes(host)
    ? { provider: null, error: `${host} isn't supported yet` }
    : null;
}

/** Validates and normalizes the `owner/repo` portion (drops a trailing `.git` and slash). */
function fromOwnerRepo(path: string): RepoParseResult {
  const cleaned = path
    .replace(/\.git$/i, '')
    .replace(/\/+$/, '')
    .replace(/^\/+/, '');

  const parts = cleaned.split('/');
  if (parts.length !== 2 || !SEGMENT.test(parts[0]) || !SEGMENT.test(parts[1]))
    return invalid();

  return { provider: 'github', repo: `${parts[0]}/${parts[1]}` };
}

/** The generic "can't read this" failure. */
function invalid(): UnparsedRepo {
  return { provider: null, error: 'Enter owner/repo or a repository URL' };
}
