import { createContext } from 'react'
import { Configuration } from '../models/Configuration'

export interface AppContext {
  configuration: Configuration
}

export const appInitialContext: AppContext = {
  configuration: {} as Configuration,
}

export const AppFeatureContext = createContext<AppContext>(appInitialContext)
