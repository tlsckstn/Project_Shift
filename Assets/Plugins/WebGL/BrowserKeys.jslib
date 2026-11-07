mergeInto(LibraryManager.library, {
    // Tab selects the next row in the game. Without this the browser also moves focus off the canvas
    // (on itch.io, out of the game iframe), and the next key presses never reach the game.
    // preventDefault only cancels the browser action; Unity's own key listener still sees the press.
    BlockBrowserTab: function () {
        window.addEventListener("keydown", function (e) {
            if (e.key === "Tab") {
                e.preventDefault();
            }
        }, true);
    }
});
