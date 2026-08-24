// Aggregates the second-pass story scenes (chapters 2-20), merged into
// each chapter's dialogue file by the generator. Scene ids follow the
// chNN_mid / chNN_mini2 / chNN_preboss convention, which the generator's
// dialogueRef map wires to stages 15, 18 and 24 automatically.
const a = require('./story_extra_a.data.js');
const b = require('./story_extra_b.data.js');
module.exports = Object.assign({}, a, b);
