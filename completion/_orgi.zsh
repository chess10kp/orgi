# Zsh completion for orgi
_orgi() {
    _arguments -C \
        '1: :->command' \
        '*:: :->args'

    case $state in
        command)
            _values 'orgi command' \
                'init[initialize orgi repository]' \
                'list[list issues]' \
                'add[add new issue]' \
                'gather[gather TODOs from source]' \
                'sync[sync issues back to source]' \
                'done[mark issue as done]' \
                '--help[show help]' \
                '--version[show version]'
            ;;
        args)
            case $line[1] in
                list)
                    _arguments \
                        '--all[list all issues]' \
                        '--open[list open issues]' \
                        '1:: :_files -g "*.org"'
                    ;;
                add)
                    _arguments \
                        '--title[title of the issue]:title:' \
                        '(--body -b)'{--body,-b}'[body text]:body:' \
                        '1:: :_files -g "*.org"'
                    ;;
                gather)
                    _arguments \
                        '--dry-run[show what would be gathered]'
                    ;;
                sync)
                    _arguments \
                        '--auto-confirm[remove without confirmation]'
                    ;;
                done)
                    _message 'index or issue ID'
                    ;;
                *)
                    _files -g "*.org"
                    ;;
            esac
            ;;
    esac
}