"""LLM integration tests: build a Visio diagram through the MCP server."""

from __future__ import annotations

import pytest
from pytest_aitest import Agent, MCPServer, Provider, Skill

from conftest import assert_regex, unique_results_path


@pytest.mark.aitest
@pytest.mark.mcp
def test_create_document_with_named_page(
    visio_mcp_server: MCPServer,
    visio_mcp_skill: Skill,
    provider: Provider,
) -> None:
    """The agent creates a .vsdx document and renames the first page."""
    output = unique_results_path("mcp-named-page")

    agent = Agent(
        servers=[visio_mcp_server],
        skills=[visio_mcp_skill],
        provider=provider,
    )
    result = agent.run(
        f"Create a new Visio document at {output}. "
        "Rename its first page to 'Overview', then save and close the document. "
        "Finish by reporting the page names you ended up with."
    )

    assert_regex(result.output, r"Overview")


@pytest.mark.aitest
@pytest.mark.mcp
def test_draw_two_shapes_and_connect_them(
    visio_mcp_server: MCPServer,
    visio_mcp_skill: Skill,
    provider: Provider,
) -> None:
    """The agent draws two labeled shapes and glues a connector between them."""
    output = unique_results_path("mcp-connected-shapes")

    agent = Agent(
        servers=[visio_mcp_server],
        skills=[visio_mcp_skill],
        provider=provider,
    )
    result = agent.run(
        f"Create a new Visio document at {output}. "
        "On the first page draw two rectangles side by side, label the left one 'Start' "
        "and the right one 'End', then connect Start to End with a connector. "
        "Verify the connection by listing the shape connections, save, and close. "
        "Report which shape each end of the connector is attached to."
    )

    assert_regex(result.output, r"Start")
    assert_regex(result.output, r"End")


@pytest.mark.aitest
@pytest.mark.mcp
def test_shapesheet_cell_roundtrip(
    visio_mcp_server: MCPServer,
    visio_mcp_skill: Skill,
    provider: Provider,
) -> None:
    """The agent writes a ShapeSheet cell and reads the value back."""
    output = unique_results_path("mcp-shapesheet")

    agent = Agent(
        servers=[visio_mcp_server],
        skills=[visio_mcp_skill],
        provider=provider,
    )
    result = agent.run(
        f"Create a new Visio document at {output}. "
        "Draw one rectangle on the first page, then set its Width ShapeSheet cell to 3 inches "
        "and read the Width cell back to confirm the change took effect. "
        "Save, close, and report the Width value you read back."
    )

    assert_regex(result.output, r"3")
