# From this repository to the App Store

**The plan:** a real iOS app, written in Swift and built in Xcode, that carries the finished game inside it and runs fully offline. The game, its painted art and its fonts are copied into the app by `ios/bundle_game.sh`. The app itself is two short Swift files in `ios/UntilWeFindHome/`.

**Why this route:** see decision 8 in `docs/architecture.md`.

**What you need:**
- A Mac with **Xcode 26 or later**. Apple requires it for every upload since April 28, 2026; see [Upcoming Requirements](https://developer.apple.com/news/upcoming-requirements/).
- Your iPhone and its cable.
- The **Apple Developer Program**, US$99 a year ([developer.apple.com/programs](https://developer.apple.com/programs/)). You only need this from part C onward. Part B is free.

---

## A. Set up the Mac (once)

1. **Install Xcode** from the Mac App Store. Open it once, accept the licence, and let it install the iOS components it offers. If the App Store says your macOS is too old for the latest Xcode, update macOS first (Apple menu, System Settings, General, Software Update).
2. **Install Homebrew**, a free installer for developer tools. Go to [brew.sh](https://brew.sh), copy the one command shown there into the Terminal app, and press Return.
3. **Install the tools the project uses:**
   ```
   brew install xcodegen git-lfs node
   ```
4. **Download the project:**
   ```
   git lfs install
   git clone https://github.com/omega1898/bodhi.git
   cd bodhi
   ```
   Until the pull request with the iOS app is merged into `main`, also run:
   ```
   git checkout claude/eloquent-hopper-el57ed
   ```
   Check: `ls ios` lists `project.yml`, `bundle_game.sh` and `UntilWeFindHome`.

## B. Run it on your iPhone (free)

5. **Choose your app ID.** Open `ios/project.yml` in TextEdit and replace both `com.yourname` with something unique to you, for example `com.yoursurname`. It becomes the app's permanent ID.
6. **Build the Xcode project:**
   ```
   sh ios/bundle_game.sh
   cd ios
   xcodegen
   open UntilWeFindHome.xcodeproj
   ```
   Check: Xcode opens with **UntilWeFindHome** in the left column.
7. **Sign in.** In Xcode, choose Settings, then Accounts, and add your Apple ID. Then click the blue project icon at the top left, choose the **UntilWeFindHome** target, open **Signing & Capabilities**, and pick your name under **Team**.
8. **Connect your iPhone** with the cable and tap **Trust** on the phone. The first time, the iPhone asks you to turn on **Developer Mode** (Settings, Privacy & Security, Developer Mode); it restarts.
9. **Run it.** At the top of Xcode, choose your iPhone as the destination and press **▶**. The game opens on your phone.
   - Check that it fills the screen in landscape, plays sound after the first tap, and still works with Wi-Fi turned off.
   - If Xcode shows a red error, copy its exact text into a cloud session. I can't build Swift in the cloud, so the first build on your Mac is its real test.

## C. Join the Apple Developer Program

10. Enrol at [developer.apple.com/programs](https://developer.apple.com/programs/) as an individual. Approval can take a day or two.

## D. Create the app in App Store Connect

11. At [appstoreconnect.apple.com](https://appstoreconnect.apple.com), choose **My Apps**, then **+**, then **New App**.
    - **Platform:** iOS.
    - **Name:** "Until We Find Home". The name must be unused on the App Store; if it's taken, try a variant.
    - **Bundle ID:** the one from step 5.
    - **SKU:** any private reference, for example `uwfh001`.

## E. Upload a build

12. In Xcode, set the destination to **Any iOS Device**, then choose **Product**, **Archive**.
13. When the Organizer window opens, choose **Distribute App**, then **App Store Connect**, then **Upload**.
14. **Every new upload** needs a higher build number. Raise `CURRENT_PROJECT_VERSION` in `ios/project.yml` (1, 2, 3...) and run `xcodegen` again.

## F. Test with real people first (TestFlight)

15. In App Store Connect, open **TestFlight** and add yourself and a few testers by email. They install the free TestFlight app and play your build.
    - This is milestone M4 in `docs/build_plan.md`: watch them play without coaching, and ask the five questions there.

## G. The store listing

16. Fill in the following in App Store Connect:
    - **Screenshots:** App Store Connect lists the sizes it needs. Take them on your iPhone during play; the volume-up and side buttons together take one.
    - **Description and keywords.**
    - **Support URL and privacy policy URL:** both are required. The privacy text is ready in `docs/privacy_policy.md`; it needs to be on a public web page. The simplest option is a free page on GitHub Pages or a site builder.
    - **App Privacy:** answer **Data Not Collected**. The game keeps progress and keepsakes only on the phone and never uses the internet.
    - **Age rating:** answer Apple's questionnaire. It includes peril (a flood and a separation) but no violence.
    - **Price.**
    - **EU trader status**, if you sell in the European Union.

## H. Submit for review

17. Choose your build, then **Add for Review**.
    - In the notes for the reviewer, say: "A complete narrative game that runs fully offline. All content is bundled in the app."
    - Apple reviews every app individually. Games built on web technology are accepted when they're a complete experience inside the app, as this one is.

## I. Before you press "Release"

- **Her written consent:** get her written agreement to appear in the game. See `docs/characters.md`.
- **The dog's real name and pronouns** in `prototype/web/js/levels.js` (`story_config`).
- **Painted scenery:** at the moment only chapter one is painted. Decide whether the first release is chapter one only (the original plan) or all six chapters.
