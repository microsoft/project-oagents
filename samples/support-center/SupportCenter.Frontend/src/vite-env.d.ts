/// <reference types="vite/client" />

interface ImportMetaEnv {
    readonly VITE_APP_TITLE: string
    readonly VITE_OAGENT_CLIENT_ID: string
    readonly VITE_OAGENT_BASE_URL: string
    readonly VITE_IS_MOCK_ENABLED: boolean
}

interface ImportMeta {
    readonly env: ImportMetaEnv
}
