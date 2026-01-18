./release.sh

gh release create v0.2.0 \
	orgi-linux-x64.tar.gz \
	--title "v0.2.0" \
	--notes "Add git worktree support for orgi data

Changes:
- Store orgi data in dedicated git worktree (.orgi/ directory)
- All orgi operations now commit to orgi-data branch
- Check for existing orgi-data branch and reuse it
- Unified PR and issue data in same version-controlled structure
- Improved data isolation and backup capabilities"
