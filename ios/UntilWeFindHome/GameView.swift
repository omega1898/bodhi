// Purpose: shows the game. The game is a folder of files (a page, JavaScript and the painted
// SVG art) bundled inside the app. This view loads them straight from the app, so the game
// works with no internet connection and sends nothing anywhere.
import SwiftUI
import WebKit

struct GameView: UIViewRepresentable {

    // Builds the game's view once, when the screen first appears.
    func makeUIView(context: Context) -> WKWebView {
        let configuration = WKWebViewConfiguration()
        // Let music and sounds play once the player has tapped, with no extra permission prompts.
        configuration.mediaTypesRequiringUserActionForPlayback = []
        configuration.allowsInlineMediaPlayback = true

        let game_view = WKWebView(frame: .zero, configuration: configuration)
        // The game's night colour sits behind it, so there's no white flash while it loads.
        game_view.isOpaque = false
        game_view.backgroundColor = UIColor(red: 13 / 255, green: 17 / 255, blue: 24 / 255, alpha: 1)
        // A game screen must not scroll, bounce or zoom under the player's thumbs.
        game_view.scrollView.isScrollEnabled = false
        game_view.scrollView.bounces = false
        game_view.scrollView.contentInsetAdjustmentBehavior = .never
        game_view.scrollView.pinchGestureRecognizer?.isEnabled = false

        #if DEBUG
        // In test builds only: lets the Mac's Safari inspect the running game, to find bugs.
        if #available(iOS 16.4, *) {
            game_view.isInspectable = true
        }
        #endif

        if let page = Bundle.main.url(forResource: "index", withExtension: "html", subdirectory: "Game") {
            // The page may read only its own folder of game files, nothing else on the phone.
            game_view.loadFileURL(page, allowingReadAccessTo: page.deletingLastPathComponent())
        }
        return game_view
    }

    // SwiftUI calls this when its own state changes. Nothing outside the game changes it, so
    // there is nothing to do here.
    func updateUIView(_ game_view: WKWebView, context: Context) {}
}
