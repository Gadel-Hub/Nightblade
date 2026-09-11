import { defineConfig } from 'vite';

export default defineConfig(({ command }) => {
  let base = '/';
  // Opt in only for a Pages build; normal development stays at the domain root.
  if (command === 'build' && process.env.GITHUB_PAGES === 'true') {
    const [owner, repository, extra] = (process.env.GITHUB_REPOSITORY ?? '').split('/');
    if (!owner || !repository || extra) throw new Error('Pages builds require GITHUB_REPOSITORY=owner/repository');
    if (!repository.toLowerCase().endsWith('.github.io')) base = `/${repository}/`;
    console.info(`GitHub Pages base: ${base}`);
  }
  return { base };
});
