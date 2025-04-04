// Funkcia na form�tovanie d�tumu do lok�lneho form�tu
function formatDate(isoDate) {
    if (!isoDate) return '';

    try {
        const date = new Date(isoDate);
        if (isNaN(date.getTime())) {
            return isoDate;
        }

        // Form�tovanie do DD.MM.YYYY
        return `${date.getDate().toString().padStart(2, '0')}.${(date.getMonth() + 1).toString().padStart(2, '0')}.${date.getFullYear()}`;
    } catch (e) {
        console.error('Chyba pri form�tovan� d�tumu:', e);
        return isoDate;
    }
}

// Funkcia na na��tanie a zobrazenie changelog �dajov z version.json
function loadVersionHistory() {
    const changelogContainer = document.getElementById('changelog-container');

    if (!changelogContainer) {
        console.error('Element s ID "changelog-container" nebol n�jden�');
        return;
    }

    // Zobrazenie na��tavania - pou�itie textContent namiesto innerHTML
    changelogContainer.textContent = 'Na��tavam hist�riu verzi�...';

    // Na��tanie version.json s�boru
    fetch('/version.json')
        .then(response => {
            if (!response.ok) {
                console.error('Chyba pri na��tan�:', response.status, response.statusText);
                throw new Error(`S�bor version.json sa nepodarilo na��ta� (${response.status})`);
            }
            return response.json();
        })
        .then(data => {
            // Kontrola, �i m�me spr�vne d�ta
            if (!data || !data.Version) {
                throw new Error('Neplatn� alebo pr�zdne d�ta vo version.json');
            }

            // Vymazanie obsahu kontajnera
            changelogContainer.textContent = '';

            // Zobrazenie aktu�lnej verzie - pou�itie textContent a createTextNode
            const currentVersionHeader = document.createElement('h1');
            currentVersionHeader.className = 'version-header current-version';

            // Vytvorenie textov�ch uzlov namiesto innerHTML
            const versionText = document.createTextNode('Aktu�lna verzia: ' + data.Version + ' ');
            currentVersionHeader.appendChild(versionText);

            const buildSpan = document.createElement('span');
            buildSpan.className = 'build-number';
            buildSpan.textContent = '(Build ' + data.BuildNumber + ')';
            currentVersionHeader.appendChild(buildSpan);

            changelogContainer.appendChild(currentVersionHeader);

            // D�tum vydania
            const currentVersionDate = document.createElement('div');
            currentVersionDate.className = 'release-date';
            currentVersionDate.textContent = 'Vydan�: ' + formatDate(data.ReleaseDate);
            changelogContainer.appendChild(currentVersionDate);

            // Zobrazenie hist�rie zmien
            const historyHeader = document.createElement('h2');
            historyHeader.textContent = 'Hist�ria zmien';
            historyHeader.className = 'history-header';
            changelogContainer.appendChild(historyHeader);

            // Vytvorenie zoznamu verzi�
            if (data.Changelog && Array.isArray(data.Changelog)) {
                data.Changelog.forEach(version => {
                    // Vytvorenie bloku pre verziu
                    const versionBlock = document.createElement('div');
                    versionBlock.className = 'version-block';

                    // Hlavi�ka verzie
                    const versionHeader = document.createElement('h3');
                    versionHeader.className = 'version-header';

                    // Vytvorenie textov�ch uzlov namiesto innerHTML
                    const versionTextNode = document.createTextNode('Verzia ' + version.Version + ' ');
                    versionHeader.appendChild(versionTextNode);

                    const buildSpan = document.createElement('span');
                    buildSpan.className = 'build-number';
                    buildSpan.textContent = '(Build ' + version.BuildNumber + ')';
                    versionHeader.appendChild(buildSpan);

                    versionBlock.appendChild(versionHeader);

                    // D�tum vydania
                    const releaseDate = document.createElement('div');
                    releaseDate.className = 'release-date';
                    releaseDate.textContent = 'Vydan�: ' + formatDate(version.ReleaseDate);
                    versionBlock.appendChild(releaseDate);

                    // Zoznam zmien
                    if (version.Changes && version.Changes.length > 0) {
                        const changesList = document.createElement('ul');
                        changesList.className = 'changes-list';

                        version.Changes.forEach(change => {
                            const changeItem = document.createElement('li');
                            // Pou�itie textContent namiesto innerText
                            changeItem.textContent = change;
                            changesList.appendChild(changeItem);
                        });

                        versionBlock.appendChild(changesList);
                    }

                    // Pridanie bloku do kontajnera
                    changelogContainer.appendChild(versionBlock);
                });
            } else {
                throw new Error('Ch�baj� �daje o hist�rii verzi�');
            }
        })
        .catch(error => {
            console.error('Chyba pri na��tan� version.json:', error);
            // Pou�itie textContent namiesto innerHTML pre zobrazenie chyby
            changelogContainer.textContent = '';
            const errorP = document.createElement('p');
            errorP.className = 'error';
            errorP.textContent = 'Nepodarilo sa na��ta� hist�riu verzi�. Detaily: ' + error.message;
            changelogContainer.appendChild(errorP);
        });
}

// Prid�me logovanie pre lep�iu diagnostiku
function debugInfo() {
    console.info('Sk��am na��ta� version.json...');
    fetch('/version.json')
        .then(response => {
            console.info('Status odpovede:', response.status, response.statusText);
            return response.text();
        })
        .then(text => {
            console.info('Obsah s�boru (prv�ch 200 znakov):', text.substring(0, 200));
            try {
                const json = JSON.parse(text);
                console.info('�spe�ne sparsovan� JSON:', json);
            } catch (e) {
                console.error('Nepodarilo sa sparsova� JSON:', e);
            }
        })
        .catch(err => {
            console.error('Chyba pri na��tan�:', err);
        });
}

// Spustenie na��tania hist�rie verzi� po na��tan� str�nky
document.addEventListener('DOMContentLoaded', () => {
    console.info('Str�nka na��tan�, za��nam na��tava� hist�riu verzi�');
    loadVersionHistory();
    debugInfo();
});