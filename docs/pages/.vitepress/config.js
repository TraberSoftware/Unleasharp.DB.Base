import { defineConfig } from "vitepress";
import { SearchPlugin } from "vitepress-plugin-search";

export default defineConfig({
  title: "Unleasharp.DB",
  description: "Unleasharp.DB Query Builder documentation",
  base: "/Unleasharp.DB.Base/docs/",

  vite: {
    plugins: [
      SearchPlugin({
        previewLength: 62,
        buttonLabel: "Search",
        placeholder: "Type to search"
      })
    ]
  },

  themeConfig: {
    logo: '/img/logo-tiny.png',
    nav: [
      { text: "🏛️ Foundation",       link: "/"                  },
      { text: "📝 Changelog",        link: "/changelog"         },
      { text: "🚀 Getting Started",  link: "/getting-started/"  },
      { text: "🗺️ Data Mapping",     link: "/data-mapping/"     },
      { text: "📋 Table Operations", link: "/table-operations/" },
      { text: "🔍 Query Building",   link: "/query-building/"   }
    ],
    sidebar: {
      "/getting-started/": [
        {
          text: "Getting Started",
          collapsed: false,
          items: [
            { text: "Index",          link: "/getting-started/index"   },
            { text: "ASP.Net",        link: "/getting-started/asp-net" },
          ]
        }
      ],
      "/data-mapping/": [
        {
          text: "Data Mapping",
          collapsed: false,
          items: [
            { text: "Index",             link: "/data-mapping/index"             },
            { text: "Simple Classes",    link: "/data-mapping/simple-classes"    },
            { text: "Annotated Classes", link: "/data-mapping/annotated-classes" },
            { text: "Joined Tables",     link: "/data-mapping/joined-tables"     },
            { text: "Enum Types",        link: "/data-mapping/enum-types"        },
          ]
        }
      ],
      "/table-operations/": [
        {
          text: "Table Operations",
          collapsed: false,
          items: [
            { text: "Index",             link: "/table-operations/index"             },
            { text: "Database-Specific", link: "/table-operations/database-specific" },
          ]
        }
      ],
      "/query-building/": [
        {
          text: "Query Building",
          collapsed: false,
          items: [
            { text: "Index",        link: "/query-building/index"        },
            { text: "Execute",      link: "/query-building/execute"      },
            { text: "Select",       link: "/query-building/select"       },
            { text: "Join",         link: "/query-building/join"         },
            { text: "Union",        link: "/query-building/union"        },
            { text: "Insert",       link: "/query-building/insert"       },
            { text: "Update",       link: "/query-building/update"       },
            { text: "Upsert",       link: "/query-building/upsert"       },
            { text: "Delete",       link: "/query-building/delete"       },
            { text: "Transactions", link: "/query-building/transactions" },
            { text: "Raw Queries",  link: "/query-building/raw"          },
            { text: "DuckDB",       link: "/query-building/duckdb"       },
          ]
        }
      ]
    }
  }
});