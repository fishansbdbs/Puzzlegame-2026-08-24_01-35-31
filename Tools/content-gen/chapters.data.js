// Aggregates the authored chapter tables (chapters 2-20).
// Chapter 1 is hand-written directly in Resources/Content and stays as-is.
module.exports = {
  list: [].concat(
    require('./chapters_a.data.js'),
    require('./chapters_b.data.js'),
    require('./chapters_c.data.js'),
    require('./chapters_d.data.js')
  ),
};
