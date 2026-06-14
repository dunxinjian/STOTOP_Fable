#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/_common.sh"

require_command dotnet "Install a .NET SDK that supports net10.0 first."

projects=(
  "tests/STOTOP.Module.Finance.Tests/STOTOP.Module.Finance.Tests.csproj"
  "tests/STOTOP.Module.CardFlow.Tests/STOTOP.Module.CardFlow.Tests.csproj"
  "tests/STOTOP.Module.Express.Tests/STOTOP.Module.Express.Tests.csproj"
)

filter="${1:-}"
selected=()
passed=()
failures=()

for project_path in "${projects[@]}"; do
  project_name="$(basename "$project_path" .csproj)"
  if [ -z "$filter" ] || [[ "$project_path" == *"$filter"* ]] || [[ "$project_name" == *"$filter"* ]]; then
    selected+=("$project_path")
  fi
done

if [ "${#selected[@]}" -eq 0 ]; then
  if [ -n "$filter" ]; then
    status_fail "no dotnet test projects selected for filter: $filter"
  else
    status_fail "no dotnet test projects selected"
  fi
  exit 1
fi

for project_path in "${selected[@]}"; do
  print_section "$project_path"

  if dotnet test "$ROOT_DIR/$project_path" -m:1 /p:UseSharedCompilation=false; then
    status_ok "$project_path"
    passed+=("$project_path")
  else
    exit_code=$?
    status_fail "$project_path (exit $exit_code)"
    failures+=("$project_path (exit $exit_code)")
  fi
done

print_section "Dotnet test summary"
status_ok "${#passed[@]} passed"

if [ "${#failures[@]}" -eq 0 ]; then
  status_ok "all selected dotnet test projects passed"
else
  status_fail "${#failures[@]} failed"
  for failure in "${failures[@]}"; do
    status_fail "$failure"
  done
  exit 1
fi
