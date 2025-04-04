// Funkcia na formátovanie dátumu do lokálneho formátu
function formatDate(isoDate) {
    if (!isoDate) return '';

    try {
        const date = new Date(isoDate);
        if (isNaN(date.getTime())) {
            return isoDate;
        }

        // Formátovanie do DD.MM.YYYY
        return `${date.getDate().toString().padStart(2, '0')}.${(date.getMonth() + 1).toString().padStart(2, '0')}.${date.getFullYear()}`;
    } catch (e) {
        console.error('Chyba pri formátovaní dátumu:', e);
        return isoDate;
    }
}

// Funkcia na naèítanie a zobrazenie changelog údajov z version.json
function loadVersionHistory() {
    const changelogContainer = document.getElementById('changelog-container');

    if (!changelogContainer) {
        console.error('Element s ID "changelog-container" nebol nájdenı');
        return;
    }

    // Zobrazenie naèítavania - pouitie textContent namiesto innerHTML
    changelogContainer.textContent = 'Naèítavam históriu verzií...';

    // Naèítanie version.json súboru
    fetch('/version.json')
        .then(response => {
            if (!response.ok) {
                console.error('Chyba pri naèítaní:', response.status, response.statusText);
                throw new Error(`Súbor version.json sa nepodarilo naèíta (${response.status})`);
            }
            return response.json();
        })
        .then(data => {
            // Kontrola, èi máme správne dáta
            if (!data || !data.Version) {
                throw new Error('Neplatné alebo prázdne dáta vo version.json');
            }

            // Vymazanie obsahu kontajnera
            changelogContainer.textContent = '';

            // Zobrazenie aktuálnej verzie - pouitie textContent a createTextNode
            const currentVersionHeader = document.createElement('h1');
            currentVersionHeader.className = 'version-header current-version';

            // Vytvorenie textovıch uzlov namiesto innerHTML
            const versionText = document.createTextNode('Aktuálna verzia: ' + data.Version + ' ');
            currentVersionHeader.appendChild(versionText);

            const buildSpan = document.createElement('span');
            buildSpan.className = 'build-number';
            buildSpan.textContent = '(Build ' + data.BuildNumber + ')';
            currentVersionHeader.appendChild(buildSpan);

            changelogContainer.appendChild(currentVersionHeader);

            // Dátum vydania
            const currentVersionDate = document.createElement('div');
            currentVersionDate.className = 'release-date';
            currentVersionDate.textContent = 'Vydaná: ' + formatDate(data.ReleaseDate);
            changelogContainer.appendChild(currentVersionDate);

            // Zobrazenie histórie zmien
            const historyHeader = document.createElement('h2');
            historyHeader.textContent = 'História zmien';
            historyHeader.className = 'history-header';
            changelogContainer.appendChild(historyHeader);

            // Vytvorenie zoznamu verzií
            if (data.Changelog && Array.isArray(data.Changelog)) {
                data.Changelog.forEach(version => {
                    // Vytvorenie bloku pre verziu
                    const versionBlock = document.createElement('div');
                    versionBlock.className = 'version-block';

                    // Hlavièka verzie
                    const versionHeader = document.createElement('h3');
                    versionHeader.className = 'version-header';

                    // Vytvorenie textovıch uzlov namiesto innerHTML
                    const versionTextNode = document.createTextNode('Verzia ' + version.Version + ' ');
                    versionHeader.appendChild(versionTextNode);

                    const buildSpan = document.createElement('span');
                    buildSpan.className = 'build-number';
                    buildSpan.textContent = '(Build ' + version.BuildNumber + ')';
                    versionHeader.appendChild(buildSpan);

                    versionBlock.appendChild(versionHeader);

                    // Dátum vydania
                    const releaseDate = document.createElement('div');
                    releaseDate.className = 'release-date';
                    releaseDate.textContent = 'Vydaná: ' + formatDate(version.ReleaseDate);
                    versionBlock.appendChild(releaseDate);

                    // Zoznam zmien
                    if (version.Changes && version.Changes.length > 0) {
                        const changesList = document.createElement('ul');
                        changesList.className = 'changes-list';

                        version.Changes.forEach(change => {
                            const changeItem = document.createElement('li');
                            // Pouitie textContent namiesto innerText
                            changeItem.textContent = change;
                            changesList.appendChild(changeItem);
                        });

                        versionBlock.appendChild(changesList);
                    }

                    // Pridanie bloku do kontajnera
                    changelogContainer.appendChild(versionBlock);
                });
            } else {
                throw new Error('Chıbajú údaje o histórii verzií');
            }
        })
        .catch(error => {
            console.error('Chyba pri naèítaní version.json:', error);
            // Pouitie textContent namiesto innerHTML pre zobrazenie chyby
            changelogContainer.textContent = '';
            const errorP = document.createElement('p');
            errorP.className = 'error';
            errorP.textContent = 'Nepodarilo sa naèíta históriu verzií. Detaily: ' + error.message;
            changelogContainer.appendChild(errorP);
        });
}

// Pridáme logovanie pre lepšiu diagnostiku
function debugInfo() {
    console.info('Skúšam naèíta version.json...');
    fetch('/version.json')
        .then(response => {
            console.info('Status odpovede:', response.status, response.statusText);
            return response.text();
        })
        .then(text => {
            console.info('Obsah súboru (prvıch 200 znakov):', text.substring(0, 200));
            try {
                const json = JSON.parse(text);
                console.info('Úspešne sparsovanı JSON:', json);
            } catch (e) {
                console.error('Nepodarilo sa sparsova JSON:', e);
            }
        })
        .catch(err => {
            console.error('Chyba pri naèítaní:', err);
        });
}

// Spustenie naèítania histórie verzií po naèítaní stránky
document.addEventListener('DOMContentLoaded', () => {
    console.info('Stránka naèítaná, zaèínam naèítava históriu verzií');
    loadVersionHistory();
    debugInfo();
});