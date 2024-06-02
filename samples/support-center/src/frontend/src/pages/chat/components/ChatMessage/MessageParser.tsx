import { Citation } from '../../../../models/Citation'
import { Message } from '../../../../models/Message'

export function parseMessage(message: Message | undefined): Message | undefined {
  if (!message) {
    return undefined
  }

  let parsedText = message.text
  const citationLinks = parsedText.match(/\[(doc\d\d?\d?)]/g)

  const lengthDocN = '[doc'.length

  const filteredCitations: Citation[] = []
  let citationReindex = 0
  citationLinks?.forEach((link) => {
    // Replacing the links/citations with number
    const citationIndex = link.slice(lengthDocN, link.length - 1)
    if (!citationIndex || !message.citations || message.citations.length < Number(citationIndex)) {
      return
    }
    const citation = { ...message.citations[Number(citationIndex) - 1] }
    if (!filteredCitations.find((c) => c.id === citationIndex)) {
      parsedText = parsedText.replaceAll(link, ` ^${++citationReindex}^ `)
      citation.id = citationIndex // original doc index to de-dupe
      citation.reindex_id = citationReindex.toString() // reindex from 1 for display
      filteredCitations.push(citation)
    }
  })

  return {
    ...message,
    text: parsedText,
    citations: filteredCitations,
  }
}
