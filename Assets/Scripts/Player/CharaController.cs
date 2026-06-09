        public override void OnSpawn()
        {
            base.OnSpawn();
            grenadeComponent = GetComponent<PlayerGrenadeComponent>();
            InitializeSpawnLoadout();
            canOpenGranade = true;
            onDamage = false;
            isBlink = false;
        }

        public override void OnReSpawn()
        {
            base.OnReSpawn();
            grenadeComponent = GetComponent<PlayerGrenadeComponent>();
            InitializeSpawnLoadout();
            canOpenGranade = true;
            onDamage = false;
            isBlink = false;
        }
