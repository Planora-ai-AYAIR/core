/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        planora: {
          // Background & surfaces
          limestone: {
            DEFAULT: '#F5F2EB',
            50: '#FDFCF9',
            100: '#F5F2EB',
            200: '#EBE4D6',
          },
          desert: {
            DEFAULT: '#E3D5C0',
            100: '#F3ECE1',
            200: '#E3D5C0',
            300: '#CBB18E',
          },
          gypsum: '#FFFFFF',
          // Primary accent - soil/clay
          clay: {
            DEFAULT: '#B86E3D',
            50: '#FBF1EA',
            100: '#F5DDCE',
            200: '#E7B99A',
            300: '#D69466',
            400: '#C47046',
            500: '#B86E3D',
            600: '#9A4E29',
            700: '#7A381E',
            800: '#5A2714',
            900: '#3A180B',
          },
          // Secondary accent - silt/nature
          silt: {
            DEFAULT: '#6B7F5E',
            50: '#F2F6F0',
            100: '#E0E9DB',
            200: '#C1D3B7',
            300: '#9BB88C',
            400: '#7E9D6A',
            500: '#6B7F5E',
            600: '#4F5F44',
            700: '#38422E',
            800: '#232A1C',
            900: '#11140D',
          },
          // Tertiary highlight - surveyor gold
          gold: {
            DEFAULT: '#C7A14D',
            100: '#FCF4E3',
            200: '#F2DDA7',
            300: '#E0BF6B',
            400: '#C7A14D',
            500: '#A87F36',
            600: '#7A5C24',
          },
          // Darks & text
          basalt: {
            DEFAULT: '#2B2D31',
            500: '#6B7280', // softer secondary text
            600: '#4B5563',
            700: '#3F4447',
            800: '#2B2D31',
            900: '#1A1C1E',
          },
          // Alerts only
          risk: {
            DEFAULT: '#A13E3A',
            100: '#FCE8E7',
            600: '#A13E3A',
          },
        },
      },
      fontFamily: {
        // Serif for English headings - feels like survey maps
        display: ['Cormorant Garamond', 'Georgia', 'serif'],
        // Cairo for Arabic + Inter for body
        sans: ['Inter', 'Cairo', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
};
