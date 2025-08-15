/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './*.html',
    './src/**/*.{html,js,jsx,ts,tsx}',
    '!./node_modules/**'
  ],
  theme: {
    extend: {
      colors: {
        primary: '#593c63',
        secondary: '#ba4c44',
        info: '#d29238',
        success: '#1ea369',
        warning: '#dccd1e',
        danger: '#e8320e',
        dark: '#0c0100',
        light: '#e3e5eb'
      }
    },
  },
  plugins: [],
}

