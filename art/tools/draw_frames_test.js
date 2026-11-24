// Purpose: break-it tests for the drawing tool's guards. The puppy and Biscuit are recoloured
// copies of Bodhi's painted sheet, found by colour. If a repaint of bodhi.svg drops one of those
// colours, the tool must stop and say so, not quietly leave the puppy or Biscuit half golden.
// Run from the repository root:  node art/tools/draw_frames_test.js
'use strict';

const assert = require('assert');
const tool = require('./draw_frames.js');

// Runs one check and prints its name, so a failure says which one.
function check(name, body) {
    body();
    console.log('ok  ' + name);
}

const definitions = tool.extract_definitions(tool.dog_sheet);

check('the painted sheet still has every colour the puppy and Biscuit swap', function () {
    tool.puppy_definitions(definitions);
    tool.check_biscuit_look(definitions);
});

check('a puppy colour missing from the sheet stops the tool', function () {
    const repainted = definitions.split('#e0a867').join('#123456');
    assert.throws(function () { tool.puppy_definitions(repainted); }, /puppy colours: #e0a867/);
});

check('a Biscuit change that finds nothing stops the tool', function () {
    // The fur filter's edge size lives only in the sheet's definitions, not in drawn frames.
    const repainted = definitions.split('scale="22"').join('scale="23"');
    assert.throws(function () { tool.check_biscuit_look(repainted); }, /biscuit_look: "scale="22""/);
});

check('Biscuit keeps no golden curl shading', function () {
    const grey = tool.biscuit_look(definitions);
    for (const golden of ['#8a5528', '#6e4220', '#c99a62']) {
        assert.strictEqual(grey.indexOf(golden), -1, golden + ' is still in Biscuit\'s coat');
    }
});
