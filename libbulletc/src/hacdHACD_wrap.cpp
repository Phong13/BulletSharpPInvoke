#include <../Extras/HACD/hacdHACD.h>

#include "hacdHACD_wrap.h"

HACD_HACD* HACD_new()
{
	return new HACD_HACD();
}

bool HACD_Compute(HACD_HACD* obj)
{
	return obj->Compute();
}

bool HACD_Compute2(HACD_HACD* obj, bool fullCH)
{
	return obj->Compute(fullCH);
}

bool HACD_Compute3(HACD_HACD* obj, bool fullCH, bool exportDistPoints)
{
	return obj->Compute(fullCH, exportDistPoints);
}

void HACD_DenormalizeData(HACD_HACD* obj)
{
	obj->DenormalizeData();
}

bool HACD_GetAddExtraDistPoints(HACD_HACD* obj)
{
	return obj->GetAddExtraDistPoints();
}

bool HACD_GetAddFacesPoints(HACD_HACD* obj)
{
	return obj->GetAddFacesPoints();
}

bool HACD_GetAddNeighboursDistPoints(HACD_HACD* obj)
{
	return obj->GetAddNeighboursDistPoints();
}
/*
const * HACD_GetCallBack(HACD_HACD* obj)
{
	return &obj->GetCallBack();
}
*/
/*
bool HACD_GetCH(HACD_HACD* obj, int numCH, HACD::Vec3* points, HACD::Vec3* triangles)
{
	return obj->GetCH(numCH, points, triangles);
}
*/
double HACD_GetCompacityWeight(HACD_HACD* obj)
{
	return obj->GetCompacityWeight();
}

double HACD_GetConcavity(HACD_HACD* obj)
{
	return obj->GetConcavity();
}

double HACD_GetConnectDist(HACD_HACD* obj)
{
	return obj->GetConnectDist();
}

int HACD_GetNClusters(HACD_HACD* obj)
{
	return obj->GetNClusters();
}

int HACD_GetNPoints(HACD_HACD* obj)
{
	return obj->GetNPoints();
}

int HACD_GetNPointsCH(HACD_HACD* obj, int numCH)
{
	return obj->GetNPointsCH(numCH);
}

int HACD_GetNTriangles(HACD_HACD* obj)
{
	return obj->GetNTriangles();
}

int HACD_GetNTrianglesCH(HACD_HACD* obj, int numCH)
{
	return obj->GetNTrianglesCH(numCH);
}

int HACD_GetNVerticesPerCH(HACD_HACD* obj)
{
	return obj->GetNVerticesPerCH();
}

const long* HACD_GetPartition(HACD_HACD* obj)
{
	return obj->GetPartition();
}
/*
const HACD::Vec3* HACD_GetPoints(HACD_HACD* obj)
{
	return obj->GetPoints();
}
*/
double HACD_GetScaleFactor(HACD_HACD* obj)
{
	return obj->GetScaleFactor();
}
/*
const HACD::Vec3* HACD_GetTriangles(HACD_HACD* obj)
{
	return obj->GetTriangles();
}
*/
double HACD_GetVolumeWeight(HACD_HACD* obj)
{
	return obj->GetVolumeWeight();
}

void HACD_NormalizeData(HACD_HACD* obj)
{
	obj->NormalizeData();
}

bool HACD_Save(HACD_HACD* obj, const char* fileName, bool uniColor)
{
	return obj->Save(fileName, uniColor);
}

bool HACD_Save2(HACD_HACD* obj, const char* fileName, bool uniColor, long numCluster)
{
	return obj->Save(fileName, uniColor, numCluster);
}

void HACD_SetAddExtraDistPoints(HACD_HACD* obj, bool addExtraDistPoints)
{
	obj->SetAddExtraDistPoints(addExtraDistPoints);
}

void HACD_SetAddFacesPoints(HACD_HACD* obj, bool addFacesPoints)
{
	obj->SetAddFacesPoints(addFacesPoints);
}

void HACD_SetAddNeighboursDistPoints(HACD_HACD* obj, bool addNeighboursDistPoints)
{
	obj->SetAddNeighboursDistPoints(addNeighboursDistPoints);
}
/*
void HACD_SetCallBack(HACD_HACD* obj, * callBack)
{
	obj->SetCallBack(*callBack);
}
*/
void HACD_SetCompacityWeight(HACD_HACD* obj, double alpha)
{
	obj->SetCompacityWeight(alpha);
}

void HACD_SetConcavity(HACD_HACD* obj, double concavity)
{
	obj->SetConcavity(concavity);
}

void HACD_SetConnectDist(HACD_HACD* obj, double ccConnectDist)
{
	obj->SetConnectDist(ccConnectDist);
}

void HACD_SetNClusters(HACD_HACD* obj, int nClusters)
{
	obj->SetNClusters(nClusters);
}

void HACD_SetNPoints(HACD_HACD* obj, int nPoints)
{
	obj->SetNPoints(nPoints);
}

void HACD_SetNTriangles(HACD_HACD* obj, int nTriangles)
{
	obj->SetNTriangles(nTriangles);
}

void HACD_SetNVerticesPerCH(HACD_HACD* obj, int nVerticesPerCH)
{
	obj->SetNVerticesPerCH(nVerticesPerCH);
}
/*
void HACD_SetPoints(HACD_HACD* obj, HACD::Vec3* points)
{
	obj->SetPoints(points);
}
*/
void HACD_SetScaleFactor(HACD_HACD* obj, double scale)
{
	obj->SetScaleFactor(scale);
}
/*
void HACD_SetTriangles(HACD_HACD* obj, HACD::Vec3* triangles)
{
	obj->SetTriangles(triangles);
}
*/
void HACD_SetVolumeWeight(HACD_HACD* obj, double beta)
{
	obj->SetVolumeWeight(beta);
}

void HACD_delete(HACD_HACD* obj)
{
	delete obj;
}