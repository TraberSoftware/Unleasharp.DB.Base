import fs from "fs";
import path from "path";

/**
 * Recursively scans a directory and generates sidebar entries
 * @param {string} dir relative to docs/
 * @returns {Array}
 */
function getSidebar(dir) {
  const fullPath = path.join(process.cwd(), "docs", dir);

  // read directory contents
  const files = fs.readdirSync(fullPath);

  // sort files: index.md first, then alphabetically
  files.sort((a, b) => {
    if (a === "index.md") return -1;
    if (b === "index.md") return 1;
    return a.localeCompare(b);
  });

  return files
    .filter((f) => f.endsWith(".md"))
    .map((file) => {
      const name = file.replace(/\.md$/, "");
      const link =
        name === "index"
          ? `/${dir}/`
          : `/${dir}/${name}`;

      return {
        text: name
          .replace(/-/g, " ")
          .replace(/\b\w/g, (c) => c.toUpperCase()), // "getting-started" → "Getting Started"
        link
      };
    });
}

export function getSidebars() {
  return {
    "/guide/": [
      {
        text: "Guide",
        collapsed: false,
        items: getSidebar("guide")
      }
    ],
    "/reference/": [
      {
        text: "Reference",
        collapsed: false,
        items: getSidebar("reference")
      }
    ],
    "/examples/": [
      {
        text: "Examples",
        collapsed: false,
        items: getSidebar("examples")
      }
    ]
  };
}
