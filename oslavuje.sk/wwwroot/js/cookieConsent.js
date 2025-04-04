// Spr�va cookie s�hlasu
(function () {
    // Funkcia na inicializ�ciu cookie managera
    function initCookieConsent() {
        // Kontrola, �i m�me ulo�en� pou��vate�sk� preferencie
        const analyticsCookieValue = getCookie("AnalyticsCookies");
        if (analyticsCookieValue !== null) {
            const checkbox = document.getElementById("analyticsCookies");
            if (checkbox) {
                checkbox.checked = analyticsCookieValue === "true";
            }
        }

        // Nastavenie event listenerov
        setupEventListeners();
    }

    // Funkcia na nastavenie event listenerov
    function setupEventListeners() {
        // Akceptovanie v�etk�ch cookies
        document.querySelector(".accept-button")?.addEventListener("click", function (e) {
            document.cookie = e.target.dataset.cookieString;
            document.cookie = "AnalyticsCookies=true; expires=Fri, 31 Dec 9999 23:59:59 GMT; path=/";
            document.getElementById("cookieConsent")?.classList.add("d-none");
            location.reload(); // Obnov� str�nku pre aplikovanie zmien
        });

        // Ulo�enie nastaven�
        document.querySelector(".save-preferences-button")?.addEventListener("click", function (e) {
            const analyticsCookies = document.getElementById("analyticsCookies")?.checked || false;
            document.cookie = e.target.dataset.cookieString;
            document.cookie = "AnalyticsCookies=" + analyticsCookies + "; expires=Fri, 31 Dec 9999 23:59:59 GMT; path=/";

            if (document.getElementById("cookieConsent")) {
                document.getElementById("cookieConsent").classList.add("d-none");
            }

            // Zatvorenie modalu
            const modal = document.getElementById('cookieSettingsModal');
            if (modal) {
                const modalInstance = bootstrap.Modal.getInstance(modal);
                modalInstance?.hide();
            }

            location.reload(); // Obnov� str�nku pre aplikovanie zmien
        });

        // Zru�enie s�hlasu s cookies
        document.querySelector(".reset-consent-button")?.addEventListener("click", function () {
            // Odstr�nenie consent cookie
            document.cookie = ".AspNet.Consent=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
            document.cookie = "AnalyticsCookies=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";

            // Obnovenie str�nky pre aplikovanie zmien
            location.reload();
        });
    }

    // Funkcia na z�skanie hodnoty cookie
    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

    // Spustenie inicializ�cie po na��tan� DOM
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCookieConsent);
    } else {
        initCookieConsent();
    }
})();