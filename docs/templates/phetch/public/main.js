import { razor } from './cshtml-razor.min.js'

export default {
  iconLinks: [
    {
      icon: 'github',
      href: 'https://github.com/jcparkyn/phetch',
      title: 'GitHub'
    },
  ],
  configureHljs: function (hljs) {
    hljs.registerLanguage("cshtml-razor", razor);
  },
}
