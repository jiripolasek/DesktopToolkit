import { existsSync, mkdirSync, readFileSync, readdirSync, unlinkSync, writeFileSync } from 'node:fs';
import path from 'node:path';

function apiSlug(name) {
  return name.toLowerCase().replace(/[^a-z0-9.]+/g, '-');
}

function normalizeHeadings(markdown) {
  let inEntry = false;
  return markdown.replace(
    /^```[^\n]*\n[\s\S]*?^```[ \t]*$|^(<a name='[^'\n]+'><\/a>\n\n)## |^### (.+)$/gm,
    (match, anchor, section) => {
      if (anchor) {
        inEntry = true;
        return anchor + '### ';
      }
      if (!section) return match;
      return (section === 'Remarks' && inEntry ? '#### ' : '## ') + section;
    },
  );
}

function readExplorerTypes(directory, routes, base) {
  const linkMap = path.join(directory, 'links.txt');
  if (!existsSync(linkMap)) throw new Error('Missing API link map at ' + linkMap + '. Run npm run build:api.');

  const types = new Map();
  const members = [];
  const namespaces = [];
  const memberKinds = { E: 'Event', F: 'Field', M: 'Method', P: 'Property' };
  const rows = readFileSync(linkMap, 'utf8').trim().split(/\r?\n/);
  for (const row of rows) {
    const match = /^(?<kind>[A-Z]):(?<fullName>[^|]+)\|(?<file>[^#|]+)(?:#(?<anchor>[^|]+))?(?:\|(?<name>.*))?$/.exec(row);
    if (!match) throw new Error('Invalid API link map entry in ' + linkMap + ': ' + row);
    const { kind, fullName, anchor, name } = match.groups;
    const file = decodeURIComponent(match.groups.file);
    const route = routes.get(file);
    if (!route) throw new Error('Unknown API link map destination ' + file + ' in ' + linkMap);
    if (kind === 'N') {
      namespaces.push(fullName);
      continue;
    }

    const title = name || fullName;
    if (kind === 'T') {
      if (anchor || types.has(file)) throw new Error('Expected one type per page for ' + file + ' in ' + linkMap);
      types.set(file, { title, fullName, url: base + '/' + route + '/', members: [] });
    } else {
      let memberKind = memberKinds[kind];
      if (kind === 'M' && /\.#c?ctor(?:\(|$)/.test(fullName)) memberKind = 'Constructor';
      if (kind === 'M' && /\.op_\w+\(/.test(fullName)) memberKind = 'Operator';
      if (!anchor || !memberKind) throw new Error('Invalid API member link in ' + linkMap + ': ' + row);
      members.push({ file, anchor: decodeURIComponent(anchor), label: title + ' ' + memberKind });
    }
  }

  if (types.size !== routes.size - 1 || types.has('index.md')) {
    throw new Error('Type links do not match the generated API pages in ' + directory);
  }
  for (const { file, anchor, label } of members) {
    const type = types.get(file);
    if (!type) throw new Error('No type link for member destination ' + file + '#' + anchor);
    type.members.push({ anchor, label });
  }
  namespaces.sort((left, right) => right.length - left.length);
  return [...types.values()].map((type) => ({
    ...type,
    namespace: namespaces.find((name) => type.fullName.startsWith(name + '.')) ?? '',
  }));
}

function wrapNamespaceTable(rows) {
  return ['<div class="api-namespace-table">', '', ...rows, '', '</div>'].join('\n');
}

function writeNamespacePages(packages, base, writePage) {
  const namespaces = new Map();
  for (const item of packages) {
    for (const type of item.types) {
      const group = namespaces.get(type.namespace) ?? [];
      group.push({ type, packageTitle: item.title });
      namespaces.set(type.namespace, group);
    }
  }

  const indexUrl = base + '/api/namespaces/';
  const index = ['| Namespace | Types |', '| --- | ---: |'];
  const frontmatter = ['pagefind: false', 'prev: false', 'next: false'];
  for (const [namespace, types] of [...namespaces].sort(([left], [right]) => left.localeCompare(right))) {
    const title = namespace || 'Global namespace';
    const slug = apiSlug(title);
    index.push('| [' + title + '](' + indexUrl + slug + '/) | ' + types.length + ' |');

    const rows = types.sort((left, right) => left.type.title.localeCompare(right.type.title))
      .map(({ type, packageTitle }) =>
        '| [`' + type.title + '`](' + type.url + ') | ' + packageTitle + ' |');
    const table = wrapNamespaceTable(['| Type | Package |', '| --- | --- |', ...rows]);
    const page = '[All namespaces](' + indexUrl + ')\n\n' + table;
    writePage('namespace-' + slug + '.md', 'api/namespaces/' + slug, title, page, frontmatter);
  }
  writePage('namespaces.md', 'api/namespaces', 'Browse by namespace', wrapNamespaceTable(index), frontmatter);
}

export function syncApiReference(site, catalog) {
  const source = path.join(site, '.api-reference');
  const output = path.join(site, 'src/content/docs/api');
  const manifest = path.join(source, 'components.json');
  const slugs = new Set(existsSync(manifest) ? JSON.parse(readFileSync(manifest, 'utf8')) : []);
  const components = catalog.filter((item) => slugs.has(item.slug));
  const base = (process.env.SITE_BASE ?? '/').replace(/\/$/, '');
  const expected = new Set();

  function writePage(filename, slug, title, body, frontmatter = []) {
    if (expected.has(filename)) throw new Error('Duplicate API reference page ' + filename);
    expected.add(filename);
    const header = ['---', 'title: ' + JSON.stringify(title), 'slug: ' + slug, ...frontmatter, '---', ''].join('\n');
    writeFileSync(path.join(output, filename), header + '\n' + body.trim() + '\n');
  }

  if (components.length) {
    mkdirSync(output, { recursive: true });
    const index = [
      '[Browse by namespace](' + base + '/api/namespaces/) or [search types and members](' + base + '/api/explorer/).',
      '',
    ];
    const explorerPackages = [];
    for (const item of components) {
      const dir = path.join(source, 'markdown', item.slug);
      const route = 'api/' + item.slug;
      const filenames = readdirSync(dir).filter((filename) => filename.endsWith('.md'));
      const routes = new Map(filenames.map((filename) => [
        filename,
        filename === 'index.md' ? route : route + '/' + apiSlug(filename.slice(0, -3)),
      ]));
      for (const [filename, slug] of routes) {
        const markdown = readFileSync(path.join(dir, filename), 'utf8').replaceAll('\r\n', '\n').trim();
        const heading = markdown.match(/^## (.+)\n/);
        if (!heading) throw new Error('Missing heading in generated API reference ' + filename);
        const isIndex = filename === 'index.md';
        const title = isIndex
          ? item.title + ' API reference'
          : heading[1].replace(/\\(.)/g, '$1').replace(/ (Class|Struct|Interface|Enum|Delegate)$/, '');
        const body = normalizeHeadings(markdown.slice(heading[0].length))
          .replace(/\]\(([^/()\s#]+\.md)(?=[#)])/g, (match, target) => {
            const destination = routes.get(decodeURIComponent(target));
            if (!destination) throw new Error('Unknown API reference link ' + target + ' in ' + filename);
            return '](' + base + '/' + destination + '/';
          })
          .replaceAll('https://learn.microsoft.com/en-us/dotnet/api/microsoft.ui.', 'https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.')
          .replaceAll('https://learn.microsoft.com/en-us/dotnet/api/windows.', 'https://learn.microsoft.com/en-us/uwp/api/windows.');
        const parent = isIndex
          ? '[Package guide](' + base + '/components/' + item.slug + '/)'
          : '[' + item.title + ' API reference](' + base + '/' + route + '/)';
        writePage(slug.slice(4).replaceAll('/', '-') + '.md', slug, title, parent + '\n\n' + body);
      }
      const types = readExplorerTypes(dir, routes, base);
      explorerPackages.push({ slug: item.slug, title: item.title, packageId: item.packageId, types });
      index.push('- [' + item.title + '](' + base + '/' + route + '/)');
    }
    writeNamespacePages(explorerPackages, base, writePage);
    writeFileSync(path.join(source, 'explorer.json'), JSON.stringify(explorerPackages));
    const explorer = "import ApiExplorer from '../../../components/ApiExplorer.astro';\n\n<ApiExplorer />";
    writePage('explorer.mdx', 'api/explorer', 'API explorer', explorer, [
      'description: Browse Desktop Toolkit API namespaces and find types and members.',
      'tableOfContents: false',
      'pagefind: false',
      'prev: false',
      'next: false',
    ]);
    writePage('index.md', 'api', 'API reference', index.join('\n'));
  }

  if (existsSync(output)) {
    for (const filename of readdirSync(output)) {
      if ((filename.endsWith('.md') || filename === 'explorer.mdx') && !expected.has(filename)) {
        unlinkSync(path.join(output, filename));
      }
    }
  }
  return new Set(components.map((item) => item.slug));
}
