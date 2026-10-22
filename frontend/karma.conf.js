const fs = require("fs");

module.exports = function (config) {
  const chrome = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
  const edge = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
  process.env.CHROME_BIN = process.env.CHROME_BIN || (fs.existsSync(chrome) ? chrome : edge);

  config.set({
    basePath: "",
    frameworks: ["jasmine"],
    plugins: [
      require("karma-jasmine"),
      require("karma-chrome-launcher"),
      require("karma-jasmine-html-reporter"),
      require("karma-coverage")
    ],
    client: { clearContext: false },
    coverageReporter: { dir: require("path").join(__dirname, "./coverage"), subdir: ".", reporters: [{ type: "text-summary" }] },
    reporters: ["progress", "kjhtml"],
    browsers: ["ChromeHeadless"],
    customLaunchers: {
      EdgeHeadless: {
        base: "ChromeHeadless",
        flags: ["--headless", "--disable-gpu"],
        chromePath: edge
      }
    },
    restartOnFileChange: false,
    singleRun: true
  });
};
