import {
  Avatar,
  Menu,
  MenuButton,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  Title3,
  makeStyles,
  shorthands,
  tokens,
} from '@fluentui/react-components'

const useStyles = makeStyles({
  navBar: {
    backgroundColor: tokens.colorBrandBackground,
    height: '40px',
    display: 'flex',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    ...shorthands.padding('10px'),
  },
  title: {
    color: tokens.colorNeutralBackground1,
  },
  userMenu: {
    display: 'flex',
    flexDirection: 'row',
    alignItems: 'center',
  },
})

export interface NavBarProps {
  userSignedIn: boolean
}

export function NavBar() {
  const styles = useStyles()
  const username = 'User'

  return (
    <div className={styles.navBar}>
      <Title3 className={styles.title}>Support Center</Title3>
        <div className={styles.userMenu}>
          <Menu>
            <MenuTrigger disableButtonEnhancement>
              <MenuButton appearance='primary'>{username}</MenuButton>
            </MenuTrigger>

            <MenuPopover>
              <MenuList>
                <MenuItem>⚙️ Settings</MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
          <Avatar color='colorful' name={username} />
        </div>
    </div>
  )
}
