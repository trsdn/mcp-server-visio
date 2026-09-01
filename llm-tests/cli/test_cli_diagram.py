"""LLM integration tests: build a Visio diagram through the CLI."""

from __future__ import annotations

import pytest
from pytest_aitest import Agent, CLIServer, Provider, Skill

from conftest import assert_cli_args_contain, assert_cli_exit_codes, unique_results_path


@pytest.mark.aitest
@pytest.mark.cli
def test_create_document_with_named_page(
    visio_cli_server: CLIServer,
    visio_cli_skill: Skill,
    provider: Provider,
) -> None:
    """The agent creates a .vsdx document and renames the first page via the CLI."""
    output = unique_results_path("cli-named-page")

    agent = Agent(
        servers=[visio_cli_server],
        skills=[visio_cli_skill],
        provider=provider,
    )
    result = agent.run(
        f"Using the visiocli command line tool, create a new Visio document at {output}. "
        "Rename its first page to 'Overview', then save and close the document. "
        "Report the page names you ended up with."
    )

    assert_cli_exit_codes(result)
    assert_cli_args_contain(result, "page")


@pytest.mark.aitest
@pytest.mark.cli
def test_draw_two_shapes_and_connect_them(
    visio_cli_server: CLIServer,
    visio_cli_skill: Skill,
    provider: Provider,
) -> None:
    """The agent draws two labeled shapes and glues a connector between them."""
    output = unique_results_path("cli-connected-shapes")

    agent = Agent(
        servers=[visio_cli_server],
        skills=[visio_cli_skill],
        provider=provider,
    )
    result = agent.run(
        f"Using the visiocli command line tool, create a new Visio document at {output}. "
        "On the first page draw two rectangles side by side, label the left one 'Start' "
        "and the right one 'End', then connect Start to End with a connector. "
        "List the shape connections to verify the result, then save and close."
    )

    assert_cli_exit_codes(result)
    assert_cli_args_contain(result, "shape")


@pytest.mark.aitest
@pytest.mark.cli
def test_shapesheet_cell_roundtrip(
    visio_cli_server: CLIServer,
    visio_cli_skill: Skill,
    provider: Provider,
) -> None:
    """The agent writes a ShapeSheet cell and reads the value back."""
    output = unique_results_path("cli-shapesheet")

    agent = Agent(
        servers=[visio_cli_server],
        skills=[visio_cli_skill],
        provider=provider,
    )
    result = agent.run(
        f"Using the visiocli command line tool, create a new Visio document at {output}. "
        "Draw one rectangle on the first page, set its Width ShapeSheet cell to 3 inches, "
        "then read the Width cell back to confirm the change. Save, close, and report the value."
    )

    assert_cli_exit_codes(result)
    assert_cli_args_contain(result, "cell")
