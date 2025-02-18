import { DateTime } from 'luxon'

export interface KnowledgeDocument {
  fileName: string
  lastModified: DateTime
  grantedUserId: string
}
