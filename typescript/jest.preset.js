/** Shared Jest config inherited by every package's jest.config.js. */
module.exports = {
  preset: "ts-jest",
  testEnvironment: "node",
  testMatch: ["**/src/**/*.test.ts"],
};
