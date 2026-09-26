import { createApp } from 'vue';
import App from './App.vue';
import { router } from './routes';
import { queryClient, queryPlugin } from './query';
import './index.css';

createApp(App).use(queryPlugin(queryClient)).use(router).mount('#root');
