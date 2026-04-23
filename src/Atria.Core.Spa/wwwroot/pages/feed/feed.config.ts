import { TabType, TabConfig } from './feed.types';
import { AtriaDataType } from '../../api/api.client';

const FEED_NAME_ADJECTIVES = [
    'Swift', 'Steamy', 'Silent', 'Rapid', 'Cosmic', 'Digital', 'Quantum',
    'Neural', 'Dynamic', 'Stellar', 'Atomic', 'Crystal', 'Thunder', 'Phoenix',
    'Shadow', 'Mystic', 'Turbo', 'Hyper', 'Ultra', 'Mega', 'Prime', 'Alpha',
    'Beta', 'Gamma', 'Delta', 'Omega', 'Nexus', 'Apex', 'Vertex', 'Matrix',
    'Blazing', 'Frozen', 'Golden', 'Silver', 'Crimson', 'Azure', 'Emerald',
    'Violet', 'Radiant', 'Luminous', 'Ethereal', 'Celestial', 'Infinite',
    'Eternal', 'Ancient', 'Modern', 'Future', 'Cyber', 'Nano', 'Micro',
    'Macro', 'Giga', 'Tera', 'Peta', 'Exo', 'Endo', 'Meta', 'Para',
    'Proto', 'Neo', 'Retro', 'Astro', 'Techno', 'Chrono', 'Pyro', 'Cryo',
    'Electro', 'Hydro', 'Aero', 'Geo', 'Bio', 'Photon', 'Neutron', 'Proton'
];

const FEED_NAME_NOUNS = [
    'Stream', 'Flow', 'Pipeline', 'Channel', 'Deck', 'Network', 'Circuit',
    'Pulse', 'Wave', 'Signal', 'Beacon', 'Relay', 'Machine', 'Gateway', 'Portal',
    'Node', 'Hub', 'Core', 'Engine', 'Reactor', 'Forge', 'Vault', 'Archive',
    'Monitor', 'Scanner', 'Tracker', 'Watcher', 'Guardian', 'Sentinel', 'Oracle',
    'Nexus', 'Bridge', 'Link', 'Chain', 'Grid', 'Mesh', 'Web', 'Net',
    'System', 'Platform', 'Framework', 'Interface', 'Protocol', 'Service',
    'Agent', 'Daemon', 'Worker', 'Handler', 'Processor', 'Analyzer', 'Parser',
    'Compiler', 'Interpreter', 'Executor', 'Scheduler', 'Dispatcher', 'Router',
    'Filter', 'Transformer', 'Aggregator', 'Collector', 'Harvester', 'Miner',
    'Explorer', 'Navigator', 'Pathfinder', 'Seeker', 'Finder', 'Locator',
    'Detector', 'Sensor', 'Probe', 'Radar', 'Sonar', 'Telescope', 'Microscope'
];

const FEED_NAME_VERBS = [
    'Running', 'Flowing', 'Streaming', 'Processing', 'Analyzing', 'Monitoring',
    'Tracking', 'Scanning', 'Watching', 'Observing', 'Detecting', 'Sensing',
    'Capturing', 'Collecting', 'Gathering', 'Harvesting', 'Mining', 'Extracting',
    'Parsing', 'Compiling', 'Executing', 'Computing', 'Calculating', 'Measuring',
    'Indexing', 'Mapping', 'Routing', 'Filtering', 'Transforming', 'Converting',
    'Encoding', 'Decoding', 'Encrypting', 'Decrypting', 'Validating', 'Verifying',
    'Authenticating', 'Authorizing', 'Synchronizing', 'Coordinating', 'Orchestrating',
    'Dispatching', 'Broadcasting', 'Multicasting', 'Relaying', 'Forwarding',
    'Aggregating', 'Consolidating', 'Merging', 'Splitting', 'Distributing'
];

export function generateFeedName(): string {
    const patterns = [
        () => {
            const adjective = FEED_NAME_ADJECTIVES[
                Math.floor(Math.random() * FEED_NAME_ADJECTIVES.length)
            ];
            const noun = FEED_NAME_NOUNS[
                Math.floor(Math.random() * FEED_NAME_NOUNS.length)
            ];
            return `${adjective} ${noun}`;
        },
        () => {
            const verb = FEED_NAME_VERBS[
                Math.floor(Math.random() * FEED_NAME_VERBS.length)
            ];
            const noun = FEED_NAME_NOUNS[
                Math.floor(Math.random() * FEED_NAME_NOUNS.length)
            ];
            return `${verb} ${noun}`;
        },
        () => {
            const adjective = FEED_NAME_ADJECTIVES[
                Math.floor(Math.random() * FEED_NAME_ADJECTIVES.length)
            ];
            const verb = FEED_NAME_VERBS[
                Math.floor(Math.random() * FEED_NAME_VERBS.length)
            ];
            return `${adjective} ${verb}`;
        }
    ];

    const selectedPattern = patterns[
        Math.floor(Math.random() * patterns.length)
    ];

    return selectedPattern();
}

export const FEED_TAB_CONFIGS: Record<TabType, TabConfig> = {
    [TabType.Settings]: {
        label: 'Settings',
        type: TabType.Settings,
        closable: false,
        requiresConfirmation: false,
        icon: 'tune'
    },
    [TabType.Filter]: {
        label: 'Filter',
        type: TabType.Filter,
        closable: true,
        requiresConfirmation: true,
        confirmationMessage: 'Are you sure you want to close the Filter tab? All unsaved changes will be lost.',
        icon: 'filter_alt'
    },
    [TabType.Function]: {
        label: 'Function',
        type: TabType.Function,
        closable: true,
        requiresConfirmation: true,
        confirmationMessage: 'Are you sure you want to close the Function tab? All unsaved changes will be lost.',
        icon: 'functions'
    },
    [TabType.Output]: {
        label: 'Output',
        type: TabType.Output,
        closable: true,
        requiresConfirmation: true,
        confirmationMessage: 'Are you sure you want to close the Output tab? All unsaved changes will be lost.',
        icon: 'settings_ethernet'
    },
    [TabType.Result]: {
        label: 'Live Preview',
        type: TabType.Result,
        closable: true,
        requiresConfirmation: false,
        confirmationMessage: 'Are you sure you want to close the Live Preview tab?',
        icon: 'sensors'
    },
    [TabType.DeployHistory]: {
        label: 'Deploy History',
        type: TabType.DeployHistory,
        closable: true,
        requiresConfirmation: false,
        icon: 'history'
    }
};

export const FILTER_TEMPLATES: Record<AtriaDataType, string> = {
    [AtriaDataType.BlockWithTransactions]: `function main(stream) {
  // Filter block transactions based on your criteria
  const txs = stream.block.transactions || [];

  const ethTransfers = txs
    .filter(x => x.gas > 500000)
    .map(tx => ({
      from: tx.from,
      to: tx.to,
      value: tx.value,
      gas: tx.gas,
      gasPrice: tx.gasPrice
    }));

  return {
    metadata: stream.metadata,
    timestamp: stream.block.timestamp,
    block: stream.block.number,
    txCount: ethTransfers.length,
    ethTransfers
  };
}`,
    [AtriaDataType.BlockWithLogs]: `function main(stream) {
  // Filter block logs based on your criteria
  const iface = new ethers.Interface([{
    name: 'Transfer', type: 'event', inputs: [
      { indexed: true, name: 'from', type: 'address' },
      { indexed: true, name: 'to', type: 'address' },
      { indexed: false, name: 'value', type: 'uint256' }
    ]
  }]);

  const topic = iface.getEvent('Transfer').topicHash;

  const parse = l => {
    try {
      if (l.data === '0x') {
        const [from, to, tokenId] = l.topics.slice(1).map(t => '0x' + t.slice(26));
        return { type:'ERC721', from, to, tokenId: BigInt(tokenId).toString(), address:l.address };
      }
      const { args } = iface.parseLog(l);
      return { type:'ERC20', from:args.from, to:args.to, value:args.value.toString(), address:l.address };
    } catch (e) { return { type:'unknown', error:e.message }; }
  };

  return {
    metadata: stream.metadata,
    transfers: stream.logs?.filter(l => l.topics?.at(0)?.toLowerCase() === topic).map(parse) || []
  };
}`,
    [AtriaDataType.BlockWithTraces]: `function main(stream) {
    // Filter debug traces based on your criteria
    return stream.metadata;
}`
};

export const FUNCTION_TEMPLATES: Record<AtriaDataType, string> = {
    [AtriaDataType.BlockWithTransactions]: `function main(stream) {
    // Transform block data
     return stream;
}`,
    [AtriaDataType.BlockWithLogs]: `function main(stream) {
    // Transform block with logs
    return stream;
}`,
    [AtriaDataType.BlockWithTraces]: `function main(stream) {
    // Transform debug trace data
    return stream;
}`
};
