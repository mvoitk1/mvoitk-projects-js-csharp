import { mkdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { build } from "esbuild";

const root = process.cwd();
const srcDir = path.join(root, "js");
const outDir = path.join(root, "dist", "js");

const files = [
  "utils.js",
  "storage.js",
  "validator.js",
  "formatters.js",
  "taskManager.js",
  "app.js"
];

await mkdir(outDir, { recursive: true });

await Promise.all(
  files.map((file) =>
    build({
      entryPoints: [path.join(srcDir, file)],
      outfile: path.join(outDir, file),
      bundle: false,
      minify: true,
      sourcemap: true,
      target: "es2018"
    })
  )
);

const htmlPath = path.join(root, "index.html");
const html = await readFile(htmlPath, "utf8");
const rewritten = html.replaceAll('src="js/', 'src="./js/');
const distHtmlPath = path.join(root, "dist", "index.html");
await writeFile(distHtmlPath, rewritten, "utf8");

console.log("Build complete: dist/index.html and dist/js/*.js");
