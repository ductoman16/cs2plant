# Aegis Framework

This directory contains the core Aegis framework implementation. Each subdirectory serves a specific purpose in maintaining project state and managing operations.

## Directory Structure

### memory/
Stores framework and project memory in a structured, file-based system:
- `core/`: Framework knowledge and operation patterns
- `project/`: Project-specific memory and state
- `session/`: Temporary session state and context

### templates/
Contains document templates for maintaining consistency:
- `tasks/`: Task definition templates
- `sessions/`: Session record templates
- `decisions/`: Decision record templates

### operations/
Stores operation patterns and command implementations:
- `core/`: Core command implementations and patterns

### tasks/
Manages tasks through their lifecycle:
- `planned/`: Tasks ready for implementation
- `active/`: Tasks currently in progress
- `hold/`: Tasks waiting on dependencies
- `completed/`: Finished tasks

## Usage
1. Copy this `.context` directory to your project root
2. Initialize with `/aegis start`
3. Begin with `/aegis plan`
