using System.Collections.Generic;
using UnityEngine;

namespace PRP.PortalSystem {
	public class SlicableMesh {

		private Mesh mesh;
		private List<Vector3> originalVertices = new List<Vector3>();
		private List<Vector3> currentVertices = new List<Vector3>();
		private List<Vector3> originalNormals = new List<Vector3>();
		private List<Vector3> currentNormals = new List<Vector3>();
		private List<int> originalIndices = new List<int>();
		private List<int> currentIndices = new List<int>();

		public SlicableMesh(Mesh baseMesh) {
			mesh = baseMesh;
			mesh.GetVertices(originalVertices);
			mesh.GetNormals(originalNormals);
			mesh.GetIndices(originalIndices, 0);
		}

		public void Revert() {
			mesh.Clear();
			mesh.SetVertices(originalVertices);
			mesh.SetNormals(originalNormals);
			mesh.SetTriangles(originalIndices, 0);
		}

		public void Slice(Plane slicingPlane) {
			if (mesh.GetTopology(0) != MeshTopology.Triangles) {
				Debug.LogWarning("Cannot slice mesh with non triangle topology!");
				return;
			}
			currentVertices.Clear();
			currentVertices.AddRange(originalVertices);
			currentNormals.Clear();
			currentNormals.AddRange(originalNormals);

			currentIndices.Clear();
			for (int i = 0; i < originalIndices.Count; i += 3) {
				int i0 = originalIndices[i];
				int i1 = originalIndices[i + 1];
				int i2 = originalIndices[i + 2];
				Vector3 v0 = originalVertices[i0];
				Vector3 v1 = originalVertices[i1];
				Vector3 v2 = originalVertices[i2];
				int nbInFront = 0;
				bool v0In = false, v1In = false, v2In = false;

				if (slicingPlane.GetSide(v0)) {
					nbInFront++;
					v0In = true;
				}
				if (slicingPlane.GetSide(v1)) {
					nbInFront++;
					v1In = true;
				}
				if (slicingPlane.GetSide(v2)) {
					nbInFront++;
					v2In = true;
				}

				Vector3 tmpV, dir, v3;
				int tmpI, i3;
				float t;
				switch (nbInFront) {
					case 3: // All in front => keep all
						currentIndices.Add(i0);
						currentIndices.Add(i1);
						currentIndices.Add(i2);
						break;
					case 2: // One behind => slice and generate two triangles
						if (!v0In) {
							tmpV = v0;
							tmpI = i0;
							v0 = v1;
							i0 = i1;
							v1 = v2;
							i1 = i2;
							v2 = tmpV;
							i2 = tmpI;
						} else if (!v1In) {
							tmpV = v0;
							tmpI = i0;
							v0 = v2;
							i0 = i2;
							v2 = v1;
							i2 = i1;
							v1 = tmpV;
							i1 = tmpI;
						} else if (!v2In) {

						}

						currentIndices.Add(i0);
						currentIndices.Add(i1);

						dir = (v2 - v0).normalized;
						slicingPlane.Raycast(new Ray(v0, dir), out t);
						i3 = currentVertices.Count;
						v3 = v0 + dir * t;
						currentIndices.Add(i3);
						currentVertices.Add(v3);
						currentNormals.Add(originalNormals[i0]); // TODO use interpolation

						currentIndices.Add(i1);

						dir = (v2 - v1).normalized;
						slicingPlane.Raycast(new Ray(v1, dir), out t);
						currentIndices.Add(currentVertices.Count);
						currentVertices.Add(v1 + dir * t);
						currentNormals.Add(originalNormals[i1]); // TODO use interpolation

						currentIndices.Add(i3);

						break;
					case 1: // Two behind => slice and generate one triangle
						if (v0In) {
							
						} else if (v1In) {
							tmpV = v0;
							tmpI = i0;
							v0 = v1;
							i0 = i1;
							v1 = v2;
							i1 = i2;
							v2 = tmpV;
							i2 = tmpI;
						} else if (v2In) {
							tmpV = v0;
							tmpI = i0;
							v0 = v2;
							i0 = i2;
							v2 = v1;
							i2 = i1;
							v1 = tmpV;
							i1 = tmpI;
						}

						currentIndices.Add(i0);

						dir = (v1 - v0).normalized;
						slicingPlane.Raycast(new Ray(v0, dir), out t);
						currentIndices.Add(currentVertices.Count);
						currentVertices.Add(v0 + dir * t);
						currentNormals.Add(originalNormals[i0]); // TODO use interpolation

						dir = (v2 - v0).normalized;
						slicingPlane.Raycast(new Ray(v0, dir), out t);
						currentIndices.Add(currentVertices.Count);
						currentVertices.Add(v0 + dir * t);
						currentNormals.Add(originalNormals[i0]); // TODO use interpolation

						break;
					case 0: // All behind => discard
						break;
				}
			}

			mesh.Clear();
			mesh.SetVertices(currentVertices);
			mesh.SetNormals(currentNormals);
			mesh.SetTriangles(currentIndices, 0);
		}

		public static implicit operator Mesh(SlicableMesh slicableMesh) {
			return slicableMesh.mesh;
		}

	}
}
