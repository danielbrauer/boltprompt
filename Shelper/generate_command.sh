#!/usr/bin/env $SHELL   

trap signal_handler SIGINT SIGTERM

signal_handler() {
    run
}

# Function to generate a custom command
generate_command() {
    shelper
    
    CUSTOM_PROMPT=$(cat /tmp/custom-command)
    # Execute the custom command
    eval $CUSTOM_PROMPT
}

run() {
	while true; do
	# Call the function to generate and run the custom command
		generate_command
	done
}

run