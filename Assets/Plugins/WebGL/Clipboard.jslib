mergeInto(LibraryManager.library, {
    // Listens for the browser's paste event (Ctrl+V). It needs no permission prompt, unlike reading the clipboard directly.
    // The text is kept until the game takes it with ClipboardTakePasted. maxLength caps it: the clipboard is outside input.
    ClipboardInit: function (maxLength) {
        if (window.nightShiftClipboard) {
            return;
        }

        window.nightShiftClipboard = { pasted: null, readBlocked: false, maxLength: maxLength };
        document.addEventListener("paste", function (e) {
            var text = e.clipboardData ? e.clipboardData.getData("text/plain") : "";
            window.nightShiftClipboard.pasted = text.slice(0, maxLength);
        }, true);
    },

    // Reads the clipboard directly (a paste button). Browsers ask for permission (Chrome, Edge: once; Firefox, Safari: a
    // confirm each time) and refuse it in an iframe that does not allow clipboard-read. A refusal sets readBlocked, so the
    // game can ask for Ctrl+V instead, which needs no permission.
    ClipboardRequestPaste: function () {
        var state = window.nightShiftClipboard;
        if (!navigator.clipboard || !navigator.clipboard.readText) {
            state.readBlocked = true;
            return;
        }

        navigator.clipboard.readText().then(function (text) {
            state.pasted = text.slice(0, state.maxLength);
        }, function () {
            state.readBlocked = true;
        });
    },

    // True once after a ClipboardRequestPaste the browser refused.
    ClipboardTakeReadBlocked: function () {
        var state = window.nightShiftClipboard;
        var blocked = state.readBlocked;
        state.readBlocked = false;
        return blocked ? 1 : 0;
    },

    // The async clipboard API first; when the browser or the page (an iframe without clipboard-write) refuses it,
    // the hidden-textarea copy, which only needs the key press that started it. The canvas gets focus back afterwards.
    ClipboardCopy: function (textPtr) {
        var text = UTF8ToString(textPtr);
        var fallback = function () {
            var area = document.createElement("textarea");
            area.value = text;
            area.style.position = "fixed";
            area.style.opacity = "0";
            document.body.appendChild(area);
            area.select();
            var copied = false;
            try {
                copied = document.execCommand("copy");
            } catch (err) {
            }

            document.body.removeChild(area);
            var canvas = document.getElementById("unity-canvas");
            if (canvas) {
                canvas.focus();
            }

            if (!copied) {
                console.warn("Clipboard copy was blocked by the browser.");
            }
        };

        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text).catch(fallback);
        } else {
            fallback();
        }
    },

    // The text pasted since the last call, or null (0) when nothing was.
    ClipboardTakePasted: function () {
        var state = window.nightShiftClipboard;
        if (!state || state.pasted === null) {
            return 0;
        }

        var text = state.pasted;
        state.pasted = null;
        var size = lengthBytesUTF8(text) + 1;
        var buffer = _malloc(size);
        stringToUTF8(text, buffer, size);
        return buffer;
    }
});
