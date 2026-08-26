function confirmDelete(id, button) {
    if (button.dataset.confirmed === "true") {
        document.getElementById("delete-" + id).submit();
        return;
    }

    button.dataset.confirmed = "true";
    button.textContent = "Confirm?";
    setTimeout(() => {
        if (button.dataset.confirmed === "true") {
            button.dataset.confirmed = "false";
            button.textContent = "Delete";
        }
    }, 3000);
}

const input = document.getElementById("photoInput");
const dropZone = document.getElementById("dropZone");
const pasteBox = document.getElementById("pasteBox");
const preview = document.getElementById("preview");

function showPreview(file) {
    if (!file || !file.type.startsWith("image/")) return;
    const reader = new FileReader();
    reader.onload = e => {
        preview.src = e.target.result;
        preview.style.display = "block";
    };
    reader.readAsDataURL(file);
}

if (input) {
    input.addEventListener("change", () => showPreview(input.files[0]));
}

if (dropZone) {
    ["dragenter", "dragover"].forEach(eventName => {
        dropZone.addEventListener(eventName, e => {
            e.preventDefault();
            dropZone.classList.add("dragging");
        });
    });

    ["dragleave", "drop"].forEach(eventName => {
        dropZone.addEventListener(eventName, e => {
            e.preventDefault();
            dropZone.classList.remove("dragging");
        });
    });

    dropZone.addEventListener("drop", e => {
        const file = e.dataTransfer.files[0];
        if (file) {
            const dt = new DataTransfer();
            dt.items.add(file);
            input.files = dt.files;
            showPreview(file);
        }
    });
}

if (pasteBox) {
    pasteBox.addEventListener("paste", e => {
        const item = [...e.clipboardData.items].find(x => x.type.startsWith("image/"));
        if (!item) return;

        const file = item.getAsFile();
        const dt = new DataTransfer();
        dt.items.add(file);
        input.files = dt.files;
        showPreview(file);
    });
}
