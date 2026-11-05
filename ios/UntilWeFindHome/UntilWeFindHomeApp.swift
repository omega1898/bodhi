// Purpose: the app's starting point. It opens one full-screen window that shows the game.
// Swift note: names from Apple's own frameworks (like WindowGroup) use Apple's style; names we
// choose ourselves follow this project's rule of full words.
import SwiftUI

@main
struct UntilWeFindHomeApp: App {
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
