import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const config: Config = {
  title: 'SGuard.ConfigValidation',
  tagline: 'Catch critical configuration issues before runtime',
  favicon: 'img/icon.png',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: true, // Improve compatibility with the upcoming Docusaurus v4
  },

  // Set the production url of your site here
  url: 'https://selcukgural.github.io',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/SGuard.ConfigValidation/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'selcukgural', // Usually your GitHub org/user name.
  projectName: 'SGuard.ConfigValidation', // Usually your repo name.

  onBrokenLinks: 'throw',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          editUrl:
            'https://github.com/selcukgural/SGuard.ConfigValidation/tree/main/docs/',
        },
        blog: false, // Disable blog
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    // Replace with your project's social card
    image: 'img/docusaurus-social-card.jpg',
    colorMode: {
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'SGuard.ConfigValidation',
      logo: {
        alt: 'SGuard.ConfigValidation Logo',
        src: 'img/icon.png',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://www.nuget.org/packages/SGuard.ConfigValidation/',
          label: 'NuGet',
          position: 'right',
        },
        {
          href: 'https://github.com/selcukgural/SGuard.ConfigValidation',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Getting Started',
              to: '/docs/intro',
            },
            {
              label: 'API Reference',
              to: '/docs/api/validators',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'GitHub Issues',
              href: 'https://github.com/selcukgural/SGuard.ConfigValidation/issues',
            },
            {
              label: 'GitHub Discussions',
              href: 'https://github.com/selcukgural/SGuard.ConfigValidation/discussions',
            },
          ],
        },
        {
          title: 'More',
          items: [
            {
              label: 'NuGet Package',
              href: 'https://www.nuget.org/packages/SGuard.ConfigValidation/',
            },
            {
              label: 'GitHub',
              href: 'https://github.com/selcukgural/SGuard.ConfigValidation',
            },
          ],
        },
      ],
      copyright: `© ${new Date().getFullYear()} Selçuk Güral. MIT License.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'json', 'yaml', 'bash'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
