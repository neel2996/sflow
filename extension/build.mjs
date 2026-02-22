import * as esbuild from "esbuild";

const watch = process.argv.includes("--watch");

const common = {
  bundle: true,
  minify: false,
  sourcemap: false,
  target: ["chrome100"],
  jsx: "automatic",
};

const entries = [
  { entryPoints: ["src/content.jsx"], outfile: "dist/content.js" },
  { entryPoints: ["src/popup.jsx"], outfile: "dist/popup.js" },
  { entryPoints: ["src/jobs.jsx"], outfile: "dist/jobs.js" },
];

if (watch) {
  for (const entry of entries) {
    const ctx = await esbuild.context({ ...common, ...entry });
    await ctx.watch();
  }
  console.log("Watching for changes...");
} else {
  for (const entry of entries) {
    await esbuild.build({ ...common, ...entry });
  }
  console.log("Build complete");
}
