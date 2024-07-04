import { makeStyles, shorthands, tokens } from '@fluentui/react-components'

export const pageStyles = makeStyles({
  sectionContainer: {
    ...shorthands.borderRadius(tokens.borderRadiusLarge),
    ...shorthands.margin('10px'),
    ...shorthands.padding('15px'),
    backgroundColor: tokens.colorNeutralBackground1,
    boxShadow: tokens.shadow4,
  },
})
