using UnityEngine;
using UnityEngine.Experimental.AI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using TWD_Components;

namespace TWD_Scripts
{
    public class ECSManager : MonoBehaviour
    {
        NavMeshQuery navMeshQuery;
        public static EntityManager manager;
        public GameObject zoombiePrefab;
        public GameObject playerPrefab;
        [SerializeField] private Camera mainCamera;

        public int numZoombie = 10;
        BlobAssetStore store;

        Entity zombie;
        Entity player;

        void Start()
        {
            store = new BlobAssetStore();
            manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, store);
            player = GameObjectConversionUtility.ConvertGameObjectHierarchy(playerPrefab, settings);
            zombie = GameObjectConversionUtility.ConvertGameObjectHierarchy(zoombiePrefab, settings);
            navMeshQuery = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, 128);

            SpawnPlayer();

            for (int i = 0; i < numZoombie; i++)
            {
                SpawnZombie();
            }
        }

        public void SpawnZombie()
        {
            var instance = manager.Instantiate(zombie);
            float3 position;
            NavMeshLocation positionInfo;
            do
            {
                float x = UnityEngine.Random.Range(400, 39600);
                float y = 8;
                float z = UnityEngine.Random.Range(400, 39600);

                position = new float3(x, y, z);

                positionInfo = navMeshQuery.MapLocation(position, Vector3.one * 3f, 0, -1);
            } while (!navMeshQuery.IsValid(positionInfo));

            quaternion rotation = quaternion.EulerYZX(0, UnityEngine.Random.Range(-180, 180), 0);

            manager.SetComponentData(instance, new Translation
            {
                Value = position
            });
            manager.SetComponentData(instance, new Rotation
            {
                Value = rotation
            });
            manager.SetComponentData(instance, new NavAgent
            {
                position = position,
                rotation = rotation,
                stoppingDistance = 10f,
                moveSpeed = UnityEngine.Random.Range(50f, 100f),
                acceleration = 50f,
                rotationSpeed = 10,
                areaMask = -1,
                status = AgentStatus.Idle,
                nextPosition = new float3
                {
                    x = Mathf.Infinity,
                    y = Mathf.Infinity,
                    z = Mathf.Infinity
                }
            });
        }

        public void SpawnPlayer()
        {
            var instance = manager.Instantiate(player);
            float3 position = new float3(1000, 8, 1000);

            quaternion rotation = quaternion.EulerYZX(0, 90, 0);

            float speed = 500f;
            float angleSpeed = 100f;

            manager.SetComponentData(instance, new Translation
            {
                Value = position
            });
            manager.SetComponentData(instance, new Rotation
            {
                Value = rotation
            });
            manager.SetComponentData(instance, new Player
            {
                speed = speed,
                angleSpeed = angleSpeed
            });
        }

        public void RespawnZombie(NavAgent agent)
        {
            float x = UnityEngine.Random.Range(400, 39600);
            float y = 8;
            float z = UnityEngine.Random.Range(400, 39600);

            float3 position = new float3(x, y, z);

            quaternion rotation = quaternion.EulerYZX(0, UnityEngine.Random.Range(-180, 180), 0);

            agent.position = position;
            agent.rotation = rotation;
        }

        public void DestroyEntity(Entity entity)
        {
            manager.DestroyEntity(entity);
        }

        private void OnDestroy()
        {
            store.Dispose();
            navMeshQuery.Dispose();
        }
    }
}
