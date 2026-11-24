// Purpose: the app's starting point. It sets how the app's sound behaves, then opens one
// full-screen window that shows the game.
// Swift note: names from Apple's own frameworks (like WindowGroup) use Apple's style; names we
// choose ourselves follow this project's rule of full words.
import SwiftUI
import AVFoundation

@main
struct UntilWeFindHomeApp: App {

    // Runs once, when the app starts, before the window appears.
    init() {
        // Without this, the game's sound follows the iPhone's silent switch, so players on silent
        // hear nothing (decision 10 in docs/architecture.md).
        // ".playback" plays sound even on silent. ".mixWithOthers" lets the player's own music or
        // podcast keep playing underneath instead of being stopped.
        // Swift note: "try?" means "attempt this; if it fails, carry on without it". If Apple
        // refuses the setting, the game still runs; it just follows the silent switch.
        try? AVAudioSession.sharedInstance().setCategory(.playback, mode: .default, options: [.mixWithOthers])
    }

    // The one window the app has: the game, edge to edge.
    var body: some Scene {
        WindowGroup {
            GameView()
                .ignoresSafeArea()                    // The game keeps clear of the notch itself.
                .statusBarHidden(true)                // No clock or battery over the scene.
                .persistentSystemOverlays(.hidden)    // The home bar fades away while playing.
        }
    }
}
