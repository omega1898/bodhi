#!/bin/sh
# Purpose: copies the finished game (prototype/web) and its art and fonts into the iOS app's
# Game folder, ready to be packed inside the app. Xcode runs this before every build, so the
# app always carries the latest game. You can also run it by hand: sh ios/bundle_game.sh
#
# It also gives the page the header a phone needs (screen size, the notch) and points it at
# the bundled fonts instead of Google's, so the app works fully offline.
set -eu

ios_folder="$(cd "$(dirname "$0")" && pwd)"
repository="$(cd "$ios_folder/.." && pwd)"
game_folder="$ios_folder/UntilWeFindHome/Game"

# Start clean, so files deleted from the game don't linger in the app.
rm -rf "$game_folder"
mkdir -p "$game_folder/js" "$game_folder/art/characters" "$game_folder/art/scenery" "$game_folder/art/animations" "$game_folder/fonts"

cp "$repository"/prototype/web/js/*.js "$game_folder/js/"
cp "$repository"/art/characters/*.svg "$game_folder/art/characters/"
cp "$repository"/art/scenery/*.svg "$game_folder/art/scenery/"
cp "$repository"/art/animations/*.svg "$game_folder/art/animations/"
cp "$repository"/art/fonts/*.woff2 "$repository"/art/fonts/fonts.css "$repository"/art/fonts/OFL_*.txt "$game_folder/fonts/"

# The page: a proper document header, the local fonts, and every line that would reach out to
# Google's font servers removed.
{
    printf '<!doctype html>\n<html lang="en">\n<head>\n<meta charset="utf-8">\n'
    printf '<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover, user-scalable=no">\n'
    printf '<link rel="stylesheet" href="fonts/fonts.css">\n</head>\n<body>\n'
    grep -v 'fonts.googleapis.com' "$repository/prototype/web/index.html" | grep -v 'fonts.gstatic.com'
    printf '</body>\n</html>\n'
} > "$game_folder/index.html"

echo "Game copied into $game_folder"
