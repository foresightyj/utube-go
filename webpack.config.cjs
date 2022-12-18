//@ts-check

/**
 * @typedef {import("webpack").Configuration} Configuration
 */

const path = require("path");
const WrapperPlugin = require("wrapper-webpack-plugin");

/** @type {Configuration} */
const config = {
  mode: "production",
  entry: "./src/App.fs.js",
  target: "node12.18",
  output: {
    path: path.join(__dirname, "dist"),
    clean: true,
    filename: "bundle.cjs",
  },
  plugins: [
    new WrapperPlugin({
      test: /\.cjs$/, // only wrap output of bundle files with '.js' extension
      afterOptimizations: true,
      header: "#!/usr/bin/env node\r\n",
    }),
  ],
  module: {},
};

module.exports = config;
