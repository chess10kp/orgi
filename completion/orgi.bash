# Bash completion for orgi
_orgi() {
    local cur prev words cword
    _get_comp_words_by_ref -n : cur prev words cword

    case $cword in
        1)
            COMPREPLY=( $(compgen -W "init list add gather sync done --help --version" -- "$cur") )
            ;;
        2)
            case ${words[1]} in
                list)
                    COMPREPLY=( $(compgen -W "--all --open" -- "$cur") )
                    ;;
                add)
                    COMPREPLY=( $(compgen -W "--title --body" -- "$cur") )
                    ;;
                gather)
                    COMPREPLY=( $(compgen -W "--dry-run" -- "$cur") )
                    ;;
                sync)
                    COMPREPLY=( $(compgen -W "--auto-confirm" -- "$cur") )
                    ;;
                done)
                    # Free input for index or ID
                    ;;
                *)
                    COMPREPLY=( $(compgen -f -X "!*.org" -- "$cur") )  # File completion for positional file
                    ;;
            esac
            ;;
        3)
            case ${words[1]} in
                add)
                    if [[ ${words[2]} == "--body" || ${words[2]} == "-b" ]]; then
                        # Body text, no completion
                        :
                    fi
                    ;;
                *)
                    COMPREPLY=( $(compgen -f -X "!*.org" -- "$cur") )
                    ;;
            esac
            ;;
        *)
            COMPREPLY=( $(compgen -f -X "!*.org" -- "$cur") )
            ;;
    esac
}

complete -F _orgi orgi