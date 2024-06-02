export interface ChatConfiguration {
  welcomeTitle: string,
  welcomeSubtitle: string,
  welcomeHints: string[]
  welcomeWarning: string,
  previewContent: 'Text' | 'File',
}

export interface Configuration {
  chatConfiguration: ChatConfiguration
}
