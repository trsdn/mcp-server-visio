import * as vscode from 'vscode';
import * as path from 'path';

/**
 * VisioMcp VS Code Extension
 *
 * This extension provides MCP server definitions for the VisioMcp MCP server,
 * enabling AI assistants like GitHub Copilot to interact with Microsoft Visio
 * through native COM automation.
 *
 * The extension bundles self-contained executables for both the MCP server and CLI -
 * no .NET SDK or runtime installation required.
 *
 * Agent Skills are registered via the chatSkills contribution point in package.json.
 */

export async function activate(context: vscode.ExtensionContext) {
	console.log('VisioMcp extension is now active');

	// Register MCP server definition provider
	context.subscriptions.push(
		vscode.lm.registerMcpServerDefinitionProvider('visio-mcp', {
			provideMcpServerDefinitions: async () => {
				// Return the MCP server definition for VisioMcp
				const extensionPath = context.extensionPath;
				const mcpServerPath = path.join(extensionPath, 'bin', 'VisioMcp.McpServer.exe');

				return [
					new vscode.McpStdioServerDefinition(
						'visio-mcp',
						mcpServerPath,
						[],
						{
							// Optional environment variables can be added here if needed
						}
					)
				];
			}
		})
	);

	// Show welcome message on first activation
	const hasShownWelcome = context.globalState.get<boolean>('visiomcp.hasShownWelcome', false);
	if (!hasShownWelcome) {
		showWelcomeMessage();
		context.globalState.update('visiomcp.hasShownWelcome', true);
	}
}

function showWelcomeMessage() {
	const message = 'VisioMcp extension activated! The Visio MCP server is now available for AI assistants.';
	const learnMore = 'Learn More';

	vscode.window.showInformationMessage(message, learnMore).then(selection => {
		if (selection === learnMore) {
			vscode.env.openExternal(vscode.Uri.parse('https://github.com/trsdn/mcp-server-visio'));
		}
	});
}

export function deactivate() {
	console.log('VisioMcp extension is now deactivated');
}
