using PlazmaGames.Core;
using PlazmaGames.Core.Utils;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
	North,
	South,
	West,
	East,
	None
}

public class MapGenerator
{
	private GenerationProperties _properties;
	private TileType[,] _grid;

	private Vector2Int _start;
	private Vector2Int _end;

	public MapGenerator(GenerationProperties props)
	{
		_properties = props;
		GenerateLayout();
		GenerateBoulders();
	}

	public (Tile[,], Tile[,]) Collect()
	{
		Tile[,] iceGrid = new Tile[_properties.size.x, _properties.size.y];
		Tile[,] stoneGrid = new Tile[_properties.size.x, _properties.size.y];

		for (int x = 0; x < _properties.size.x; x++)
		{
			for (int y = 0; y < _properties.size.y; y++)
			{
				iceGrid[x, y] = new Tile(TileType.Ice);
				if (x == 0 || y == 0 || x == _properties.size.x - 1 || y == _properties.size.y - 1) iceGrid[x, y].type = TileType.Water;


				stoneGrid[x, y] = new Tile(TileType.Floor);
				if (_grid[x, y] == TileType.Boulder)
				{
					stoneGrid[x, y].hasBoulder = true;
				}
				else if (_grid[x, y] == TileType.End)
				{
					stoneGrid[x, y].type = TileType.Wall;
					iceGrid[x, y].type = TileType.End;
                }
                else if (
					_grid[x, y] == TileType.Hole ||
					_grid[x, y] == TileType.Wall
				) {
					stoneGrid[x, y].type = _grid[x, y];
				}
			}
		}

		return (iceGrid, stoneGrid);
	}

	private Vector2Int GetNextTile(Vector2Int cur, Direction dir)
	{
		if (dir == Direction.North)
		{
			return cur + new Vector2Int(0, 1);
		}
		else if (dir == Direction.South)
		{
			return cur + new Vector2Int(0, -1);
		}
		else if (dir == Direction.East)
		{
			return cur + new Vector2Int(1, 0);
		}
		else
		{
			return cur + new Vector2Int(-1, 0);
		}
	}

	private Vector2Int GetLastTile(Vector2Int cur, Direction dir)
	{
		if (dir == Direction.North)
		{
			return cur + new Vector2Int(0, -1);
		}
		else if (dir == Direction.South)
		{
			return cur + new Vector2Int(0, 1);
		}
		else if (dir == Direction.East)
		{
			return cur + new Vector2Int(-1, 0);
		}
		else
		{
			return cur + new Vector2Int(1, 0);
		}
	}

	private Direction GetDirection(Vector2Int cur, Direction? curDir = null)
	{
		int dir = -1;

		int maxTries = 100;
		int numTries = 0;

		while (dir < 0)
		{
			numTries++;

			dir = Random.Range(0, 4);

			Vector2Int next = GetNextTile(cur, (Direction)dir);
			Vector2Int next2 = GetNextTile(next, (Direction)dir);

			if 
			(
				next2.x < 0 ||
				next2.y < 0 ||
				next2.x >= _properties.size.x ||
				next2.y >= _properties.size.y ||
				_grid[next.x, next.y] != TileType.None ||
				_grid[next2.x, next2.y] != TileType.None ||
				curDir == (Direction)dir
			)
			{
				if (numTries > maxTries) return Direction.None;

				dir = -1;
				continue;
			}
		}

		return (Direction)dir;
	}


	private Direction GetDirection(Vector2Int cur, out Vector2Int next, out Vector2Int next2, Direction? curDir = null)
	{
		int dir = -1;

		int maxTries = 100;
		int numTries = 0;

		next = cur;
		next2 = cur;

		while (dir < 0)
		{
			numTries++;

			dir = Random.Range(0, 4);

			next = GetNextTile(cur, (Direction)dir);
			next2 = GetNextTile(next, (Direction)dir);

			if
			(
				next2.x < 0 ||
				next2.y < 0 ||
				next2.x >= _properties.size.x ||
				next2.y >= _properties.size.y ||
				!(_grid[next.x, next.y] == TileType.None || _grid[next.x, next.y] == TileType.Path || _grid[next.x, next.y] == TileType.Push) ||
				!(_grid[next2.x, next2.y] == TileType.None || _grid[next2.x, next2.y] == TileType.Path || _grid[next2.x, next2.y] == TileType.Push) ||
				curDir == (Direction)dir
			)
			{
				if (numTries > maxTries) return Direction.None;

				dir = -1;
				continue;
			}
		}

		return (Direction)dir;
	}

	private bool CanMove(Vector2Int cur, Direction dir)
	{
		Vector2Int next = GetNextTile(cur, dir);
		Vector2Int next2 = GetNextTile(next, dir);

		if 
		(
			next2.x < 0 || 
			next2.y < 0 || 
			next2.x >= _properties.size.x || 
			next2.y >= _properties.size.y ||
			_grid[next.x, next.y] != TileType.None ||
			_grid[next2.x, next2.y] != TileType.None
		) return false;

		return true;
	}
	
	private bool HasSurroundingHole(Vector2Int pos)
	{
		return _grid[pos.x + 1, pos.y] == TileType.Hole || _grid[pos.x - 1, pos.y] == TileType.Hole || _grid[pos.x, pos.y + 1] == TileType.Hole || _grid[pos.x, pos.y - 1] == TileType.Hole;
	}

	private void PlaceHold(Vector2Int loc, ref Vector2Int cur, Direction dir)
	{
		bool hasPlaced = false;

		do {
			if (!HasSurroundingHole(loc))
			{
				_grid[loc.x, loc.y] = TileType.Hole;
				hasPlaced = true;
			}
			else
			{
				loc = GetLastTile(loc, dir);
				cur = GetLastTile(loc, dir);
			}

		} while (!hasPlaced);
	}
	private void GenerateLayout()
	{
		_grid = ArrayUtilities.CreateAndFill(_properties.size.x, _properties.size.y, TileType.None);

		for (int i = 0; i < _grid.GetLength(0); i++)
		{
			for (int j = 0; j < _grid.GetLength(1); j++)
			{
				if (i == 0 || j == 0 || i == _grid.GetLength(0) - 1 || j == _grid.GetLength(1) - 1)
				{
					_grid[i, j] = TileType.Wall;
				}
			}
		}

		_start = new Vector2Int(1, 1);
		float rand = Random.value;
		_end = new Vector2Int(
			(rand >= 0.5f) ? _grid.GetLength(0) - 3 : _grid.GetLength(0) - 1,
			(rand >= 0.5f) ? _grid.GetLength(1) - 1 : _grid.GetLength(1) - 3
		);

		_grid[_start.x, _start.y] = TileType.Start;
		_grid[_end.x, _end.y] = TileType.End;

		Vector2Int cur = _start;
		Direction curDir = GetDirection(_start, null);
		int holesPlaced = 0;
		int numberOfMoves = 0;
		while (holesPlaced < _properties.minNumBoulders && cur != _end)
		{
			if ((Random.value < 1 - _properties.turnProb || numberOfMoves <= 2) && CanMove(cur, curDir))
			{
				numberOfMoves++;
				cur = GetNextTile(cur, curDir);
			}
			else
			{
				holesPlaced++;
				if (_grid[GetNextTile(cur, curDir).x, GetNextTile(cur, curDir).y] == TileType.End)
				{
					cur = GetLastTile(cur, curDir);
					PlaceHold(GetNextTile(cur, curDir), ref cur, curDir);
				}
				else
				{
					PlaceHold(GetNextTile(cur, curDir), ref cur, curDir);
				}
				numberOfMoves = 0;
				curDir = GetDirection(cur, curDir);
			}
		}

		if (cur != _end)
		{
			if (cur.y != _end.y && _end.y != _properties.size.y - 1)
			{
				int moves = -(cur.y - _end.y);

				cur += new Vector2Int(0, moves);
				curDir = (moves > 0) ? Direction.North : Direction.South;
				_grid[GetNextTile(cur, curDir).x, GetNextTile(cur, curDir).y] = TileType.Hole;
			}
			else if (cur.x != _end.x && _end.x != _properties.size.x - 1)
			{
				int moves = -(cur.x - _end.x);

				cur += new Vector2Int(moves, 0);
				curDir = (moves > 0) ? Direction.East : Direction.West;
				_grid[GetNextTile(cur, curDir).x, GetNextTile(cur, curDir).y] = TileType.Hole;
			}
		}
	}


	private void PlaceBoulders(Vector2Int loc)
	{
		Vector2Int orig = loc;
		Vector2Int move = loc;
		Vector2Int move2 = move;
		Direction dir = GetDirection(loc, out move, out move2);
		TileType t = _grid[loc.x, loc.y];
		do
		{
			Direction dirLast = dir;

			if (dir == Direction.None) break;

			if (_grid[loc.x, loc.y] != TileType.Hole) _grid[loc.x, loc.y] = TileType.Path;
			_grid[move.x, move.y] = TileType.Boulder;
			_grid[move2.x, move2.y] = TileType.Push;

			loc = move;
			dir = GetDirection(loc, out move, out move2);

		} while (Random.value > 1 - _properties.boulderMoveProb);

		if (dir != Direction.None) _grid[move2.x, move2.y] = TileType.Push;

		Debug.Log(_grid[orig.x, orig.y] == t);

		if (!(_grid[orig.x, orig.y] == t)) Debug.Log(_grid[orig.x, orig.y]);
	}

	private void PlacePath(Vector2Int loc)
	{
		PathFinder pf = new PathFinder(_grid);

		Dictionary<Vector2Int, Vector2Int> cameFrom = pf.FindOptimalPath(
            //new Vector2Int(Mathf.FloorToInt(_properties.size.x / 2f), Mathf.FloorToInt(_properties.size.y / 2f)),
            _start,
            loc
		);

        //_grid[Mathf.FloorToInt(_properties.size.x / 2f), Mathf.FloorToInt(_properties.size.y / 2f)] = TileType.Floor;
        _grid[_start.x, _start.y] = TileType.Floor;

        Vector2Int gridPT = loc;
		while (true)
		{
			if (_grid[gridPT.x, gridPT.y] == TileType.None) _grid[gridPT.x, gridPT.y] = TileType.Floor;
			if (!cameFrom.ContainsKey(gridPT)) break;
			else gridPT = cameFrom[gridPT];
		}
	}

	private void GenerateBoulders()
	{
		for (int i = 0; i < _grid.GetLength(0); i++)
		{
			for (int j = 0; j < _grid.GetLength(1); j++)
			{
				if (_grid[i, j] == TileType.Hole)
				{
					PlaceBoulders(new Vector2Int(i, j));
				}
			}
		}

		for (int i = 0; i < _grid.GetLength(0); i++)
		{
			for (int j = 0; j < _grid.GetLength(1); j++)
			{
				if (_grid[i, j] == TileType.Push)
				{
					PlacePath(new Vector2Int(i, j));
				}
			}
		}

		for (int i = 0; i < _grid.GetLength(0); i++)
		{
			for (int j = 0; j < _grid.GetLength(1); j++)
			{
				if (_grid[i, j] == TileType.None)
				{
					_grid[i, j] = TileType.Wall;
				}
			}
		}
	}
}
