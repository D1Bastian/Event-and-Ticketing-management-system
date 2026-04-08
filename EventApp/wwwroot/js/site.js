document.addEventListener("DOMContentLoaded", () => {
    const confettiRoot = document.querySelector("[data-confetti]");
    if (!confettiRoot) {
        return;
    }

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    if (reducedMotion.matches) {
        return;
    }

    const colors = ["#0d6efd", "#198754", "#ffc107", "#dc3545", "#6f42c1"];

    for (let index = 0; index < 32; index += 1) {
        const piece = document.createElement("span");
        piece.className = "confetti-piece";
        piece.style.left = `${Math.random() * 100}%`;
        piece.style.backgroundColor = colors[index % colors.length];
        piece.style.animationDelay = `${Math.random() * 0.25}s`;
        piece.style.animationDuration = `${2.2 + Math.random() * 1.2}s`;
        piece.style.transform = `translateY(-20px) rotate(${Math.random() * 360}deg)`;
        confettiRoot.appendChild(piece);
    }
});
