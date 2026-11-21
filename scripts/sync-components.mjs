import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, readdirSync, unlinkSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { repositoryUrl } from '../site-links.mjs';
import { syncApiReference } from './sync-api-reference.mjs';

const site = fileURLToPath(new URL('../', import.meta.url));

function mainWorktree() {
  const worktrees = execFileSync('git', ['worktree', 'list', '--porcelain'], { cwd: site, encoding: 'utf8' });
  for (const entry of worktrees.trim().split(/\r?\n\r?\n/)) {
    const lines = entry.split(/\r?\n/);
    if (lines.includes('branch refs/heads/main')) return lines[0].slice('worktree '.length);
  }
  throw new Error('Set COMPONENTS_ROOT to a checkout of the library branch.');
}

const root = path.resolve(process.env.COMPONENTS_ROOT || mainWorktree());
const catalog = JSON.parse(execFileSync(
  'pwsh', ['-NoProfile', '-File', path.join(root, 'eng/Get-Components.ps1'), '-Json'],
  { encoding: 'utf8' },
));
const apiComponents = syncApiReference(site, catalog);
const output = path.join(site, 'src/content/docs/components');
const quickStarts = path.join(site, 'src/content/quick-starts');
const localPreviewInstall = readFileSync(path.join(quickStarts, 'local-preview-install.md'), 'utf8').trim();
mkdirSync(output, { recursive: true });
const expectedFiles = new Set();
for (const item of catalog) {
  const filename = item.slug + '.md';
  const readme = readFileSync(path.join(root, path.dirname(item.project), 'README.md'), 'utf8');
  const licenseUrl = repositoryUrl + '/blob/main/' + path.posix.join(path.posix.dirname(item.project), '../LICENSE');
  const guide = readme
    .replace(/^# [^\r\n]+\r?\n/, '')
    .trim()
    .replaceAll('](../LICENSE)', '](' + licenseUrl + ')');
  const quickStartPath = path.join(quickStarts, filename);
  const quickStart = existsSync(quickStartPath)
    ? readFileSync(quickStartPath, 'utf8')
        .trim()
        .replace('{{installLocalPreview}}', localPreviewInstall)
        .replaceAll('{{project}}', item.project)
        .replaceAll('{{packageId}}', item.packageId)
        .replaceAll('{{version}}', item.version)
    : '';
  const page = [
    '---', 'title: ' + JSON.stringify(item.title), 'description: ' + JSON.stringify(item.description), '---', '',
    '**Package** `' + item.packageId + '`  ', '**Version** `' + item.version + '` (' + item.stage + ')', '',
    ...(apiComponents.has(item.slug) ? ['[API reference](../../api/' + item.slug + '/)', ''] : []),
    ...(quickStart ? [quickStart, '', '## Reference', ''] : []),
    guide,
    '',
  ].join('\n');
  writeFileSync(path.join(output, filename), page.replace(/\r\n/g, '\n'));
  expectedFiles.add(filename);
}
for (const filename of readdirSync(output)) {
  if (/^[a-z0-9-]+\.md$/.test(filename) && !expectedFiles.has(filename)) unlinkSync(path.join(output, filename));
}
const packageIndex = [
  '---',
  'title: Packages',
  'description: Package IDs, versions, and guides for JPSoftworks Desktop Toolkit.',
  '---',
  '',
  'Use the package ID and version in your project. Each guide describes the supported app targets and shows how to use the package.',
  '',
];
for (const item of catalog) {
  packageIndex.push(
    '## [' + item.title + '](../components/' + item.slug + '/)',
    '',
    item.description.replaceAll('WinUI 3', 'WinUI&nbsp;3'),
    '',
    '**Package ID** `' + item.packageId + '`  ',
    '**Version** `' + item.version + '` (' + item.stage + ')',
    '',
  );
}
writeFileSync(path.join(output, '../packages.md'), packageIndex.join('\n'));
console.log('Generated the package index and ' + expectedFiles.size + ' component guide(s).');
