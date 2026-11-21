import { existsSync } from 'node:fs';
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import { repositoryUrl, websiteUrl } from './site-links.mjs';

export default defineConfig({
  site: process.env.SITE_ORIGIN || 'https://desktoptoolkit.jiripolasek.com',
  base: process.env.SITE_BASE ?? '/',
  redirects: {
    '/components/date-range-picker': '/components/date-time/',
    '/api/date-range-picker': '/api/date-time/',
    '/api/date-range-picker/jpsoftworks.desktoptoolkit.controls.daterange': '/api/date-time/jpsoftworks.desktoptoolkit.controls.daterange/',
    '/api/date-range-picker/jpsoftworks.desktoptoolkit.controls.daterangepicker': '/api/date-time/jpsoftworks.desktoptoolkit.controls.calendardaterangepicker/',
    '/api/date-range-picker/jpsoftworks.desktoptoolkit.controls.daterangepreset': '/api/date-time/jpsoftworks.desktoptoolkit.controls.daterangepreset/',
    '/api/date-range-picker/jpsoftworks.desktoptoolkit.controls.daterangeselection': '/api/date-time/jpsoftworks.desktoptoolkit.controls.daterangeselection/',
  },
  devToolbar: { enabled: false },
  integrations: [
    starlight({
      title: 'JPSoftworks Desktop Toolkit',
      favicon: '/favicon.ico?v=2',
      logo: { src: './src/assets/toolkit-icon.png', alt: '' },
      customCss: ['./src/styles/toolkit.css'],
      expressiveCode: {
        themes: ['dark-plus', 'light-plus'],
        useStarlightUiThemeColors: true,
      },
      components: {
        SocialIcons: './src/components/ProjectLinks.astro',
        Footer: './src/components/ToolkitFooter.astro',
      },
      social: [
        { icon: 'external', label: 'Website', href: websiteUrl },
        { icon: 'github', label: 'GitHub', href: repositoryUrl },
      ],
      sidebar: [
        { label: 'Getting started', slug: 'getting-started' },
        { label: 'Localization', slug: 'localization' },
        { label: 'Control catalog', slug: 'catalog' },
        {
          label: 'Packages',
          items: [
            { label: 'Package index', slug: 'packages' },
            { autogenerate: { directory: 'components' } },
          ],
        },
        ...(existsSync(new URL('./src/content/docs/api/explorer.mdx', import.meta.url)) ? [{
          label: 'API reference',
          items: [
            { slug: 'api/explorer' },
            { slug: 'api/namespaces' },
            { label: 'Browse by package', slug: 'api' },
          ],
        }] : []),
        { label: 'Developing a component', slug: 'development' },
      ],
    }),
  ],
});
