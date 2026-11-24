# The public pages Apple asks for

App Store Connect needs two web addresses before you can submit the game: a **privacy policy URL** and a **support URL**. These files are those two pages, ready to put online.

| File | What it is |
|---|---|
| `privacy.html` | The privacy policy. Same words as `docs/privacy_policy.md`; change both together. |
| `support.html` | The support page: how to reach you, and three common questions. |
| `style.css` | The look of both pages. Light or dark to match the reader's phone. |

They use no outside files, fonts or scripts, so they load anywhere and track no one.

## Before you publish

Replace `SUPPORT_EMAIL_ADDRESS` in both HTML files with the address you want players to write to. Apple shows the support page to anyone who looks at the game in the store, so consider an address made for the game rather than your personal one.

## Where to publish them

The pages are plain files, so any free static host works. Two options:

- **GitHub Pages from a new, public repository** (free). Create a public repository, for example `untilwefindhome-site`, upload these three files, then in its Settings, Pages, choose **Deploy from a branch**, `main`, `/ (root)`. The addresses become `https://YOUR-GITHUB-NAME.github.io/untilwefindhome-site/privacy.html` and `.../support.html`.
  - Why not from this repository: it's private, and [GitHub Pages](https://docs.github.com/en/pages/getting-started-with-github-pages/what-is-github-pages) publishes from private repositories only on paid plans (GitHub Pro and up). A separate public repository holds only these three files, so nothing else of the game becomes public.
- **Any site builder or host you already use**: upload the three files as they are.

Check: open both addresses on your phone. Both pages should load, the email link should open Mail, and the links between the two pages should work.
