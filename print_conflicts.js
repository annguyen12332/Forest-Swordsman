const fs = require('fs');

function printConflicts(filename) {
    let content = fs.readFileSync(filename, 'utf-8');
    const lines = content.replace(/\r\n/g, '\n').split('\n');

    let inConflict = false;
    let head = [];
    let main = [];
    let state = 'NORMAL';
    let conflicts = [];

    for (let line of lines) {
        if (line.startsWith('<<<<<<< HEAD')) {
            state = 'HEAD';
            head = [];
            main = [];
            continue;
        } else if (line.startsWith('=======')) {
            state = 'MAIN';
            continue;
        } else if (line.startsWith('>>>>>>>')) {
            state = 'NORMAL';
            conflicts.push({ head, main });
            continue;
        }

        if (state === 'HEAD') head.push(line);
        else if (state === 'MAIN') main.push(line);
    }

    conflicts.forEach((c, idx) => {
        console.log(`--- Conflict ${idx + 1} ---`);
        console.log(`HEAD:\n${c.head.join('\n')}`);
        console.log(`MAIN:\n${c.main.join('\n')}`);
        console.log('-------------------\n');
    });
}

printConflicts(process.argv[2]);
