import {defineConfig} from 'vite';
import react from '@vitejs/plugin-react';
export default defineConfig({plugins:[react()],base:'/spa/',server:{port:5173,proxy:{'/api':{target:'https://localhost:7157',secure:false},'/auth':{target:'https://localhost:7157',secure:false}}}});
