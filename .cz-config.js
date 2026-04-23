module.exports = {
    types: [
        { value: 'feat', name:      'feat:      A new feature' },
        { value: 'fix', name:       'fix:       A bug fix' },
        { value: 'chore', name:     'chore:     Changes to auxiliary tools or libraries' },
        { value: 'ci', name:        'ci:        Changes to CI or build configuration' },
        { value: 'docs', name:      'docs:      Changes to documentation' },
        { value: 'refactor', name:  'refactor:  Code refactoring', },
        { value: 'test', name:      'test:      Adding missing or correcting existing tests' }
    ],
    messages: {
        type: "Select the type of change that you're committing:",
        subject: 'Write a SHORT, IMPERATIVE tense description of the change:\n',
        breaking: 'List any BREAKING CHANGES (optional):\n',
        confirmCommit: 'Are you sure you want to proceed with the commit above?',
    },
    allowCustomScopes: false,
    allowBreakingChanges: ['feat', 'fix'],
    skipQuestions: ['body', 'scope', 'footer'],
    subjectLimit: 100
};