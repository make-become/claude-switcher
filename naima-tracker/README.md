[Naima](https://github.com/vincenzoml/naima) is a silent software house of AI agents: it turns vibe coding into an exact science.

`naima/`, the program, is a git clone of https://github.com/vincenzoml/naima.git that git ignores; `naima-data/naima.json` locks its commit. A fresh clone of this project restores it with:

    git clone https://github.com/vincenzoml/naima.git naima-tracker/naima
    deno run -A naima-tracker/naima/naima.ts check
